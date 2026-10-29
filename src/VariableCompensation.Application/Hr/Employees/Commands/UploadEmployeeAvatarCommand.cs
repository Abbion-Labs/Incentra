using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Abstractions.Storage;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain;

namespace VariableCompensation.Application.Hr.Employees.Commands;

public sealed record UploadEmployeeAvatarCommand(long EmployeeId, Stream Content, string ContentType, long ContentLength)
    : IRequest<Result<EmployeeResponse>>;

public sealed class UploadEmployeeAvatarCommandHandler : IRequestHandler<UploadEmployeeAvatarCommand, Result<EmployeeResponse>>
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private readonly IEmployeeRepository employeeRepository;
    private readonly IEmployeeAvatarStorage avatarStorage;
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;
    private readonly ILogger<UploadEmployeeAvatarCommandHandler> logger;

    public UploadEmployeeAvatarCommandHandler(
        IEmployeeRepository employeeRepository,
        IEmployeeAvatarStorage avatarStorage,
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext,
        ILogger<UploadEmployeeAvatarCommandHandler> logger)
    {
        this.employeeRepository = employeeRepository;
        this.avatarStorage = avatarStorage;
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
        this.logger = logger;
    }

    public async Task<Result<EmployeeResponse>> Handle(UploadEmployeeAvatarCommand request, CancellationToken cancellationToken)
    {
        if (request.ContentLength <= 0)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.ImageRequired);
        }

        if (request.ContentLength > MaxFileSizeBytes)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.ImageTooLarge);
        }

        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.ImageTypeUnsupported);
        }

        var employee = await this.employeeRepository.FindByIdForUpdateAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeNotFound);
        }

        var access = await EmployeeAvatarAccess.EnsureCanManageAsync(
            employee,
            this.currentUserService,
            this.currentEmployeeContext,
            cancellationToken);
        if (access.IsFailure)
        {
            return Result.Failure<EmployeeResponse>(access.Error);
        }

        // The current picture goes only once the new one is stored and recorded: any failure before that leaves the
        // employee with the picture they had.
        var previousAvatarUrl = employee.AvatarUrl;

        string avatarUrl;
        try
        {
            avatarUrl = await this.avatarStorage.SaveAsync(request.EmployeeId, request.Content, request.ContentType, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // The storage's own message is for the log: it names the provider and its status codes.
            this.logger.LogError(ex, "Avatar upload failed for employee {EmployeeId}", request.EmployeeId);
            return Result.Failure<EmployeeResponse>(ErrorCodes.ImageUploadFailed);
        }

        employee.AvatarUrl = avatarUrl;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedByUserId = this.currentUserService.UserId;

        try
        {
            await this.employeeRepository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await AvatarCleanup.DeleteQuietlyAsync(this.avatarStorage, avatarUrl, this.logger);
            throw;
        }

        await AvatarCleanup.DeleteQuietlyAsync(this.avatarStorage, previousAvatarUrl, this.logger);

        var updated = await this.employeeRepository.FindByIdAsync(request.EmployeeId, cancellationToken);
        return Result.Success(HrMappings.ToResponse(updated!));
    }
}
