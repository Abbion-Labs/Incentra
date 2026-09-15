using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Abstractions.Storage;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain;

namespace VariableCompensation.Application.Hr.Employees.Commands;

public sealed record DeleteEmployeeAvatarCommand(long EmployeeId) : IRequest<Result<EmployeeResponse>>;

public sealed class DeleteEmployeeAvatarCommandHandler : IRequestHandler<DeleteEmployeeAvatarCommand, Result<EmployeeResponse>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly IEmployeeAvatarStorage avatarStorage;
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;

    public DeleteEmployeeAvatarCommandHandler(
        IEmployeeRepository employeeRepository,
        IEmployeeAvatarStorage avatarStorage,
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext)
    {
        this.employeeRepository = employeeRepository;
        this.avatarStorage = avatarStorage;
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
    }

    public async Task<Result<EmployeeResponse>> Handle(DeleteEmployeeAvatarCommand request, CancellationToken cancellationToken)
    {
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

        await this.avatarStorage.DeleteIfExistsAsync(employee.AvatarUrl, cancellationToken);
        employee.AvatarUrl = null;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedByUserId = this.currentUserService.UserId;

        await this.employeeRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.employeeRepository.FindByIdAsync(request.EmployeeId, cancellationToken);
        return Result.Success(HrMappings.ToResponse(updated!));
    }
}
