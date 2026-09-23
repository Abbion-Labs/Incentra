using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Application.Hr.Models;
using EvaluatorSettingsEntity = VariableCompensation.Domain.Entities.Hr.EvaluatorSettings;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Hr.EvaluatorSettings.Commands;

/// <summary>
/// Evaluator settings are created by granting the EVALUATOR role, never on
/// their own: a settings row without the role would be an evaluator who
/// cannot sign in and rate anyone, which is the drift this design removes.
/// Only the controller can be changed here; rating bands and their shares
/// are organisation-wide and live on the descriptive ratings.
/// </summary>
public sealed record UpdateEvaluatorSettingsCommand(
    long EmployeeId,
    long? ControllerEmployeeId,
    int? Version) : IRequest<Result<EvaluatorSettingsResponse>>;

public sealed class UpdateEvaluatorSettingsCommandHandler : IRequestHandler<UpdateEvaluatorSettingsCommand, Result<EvaluatorSettingsResponse>>
{
    private readonly IEvaluatorSettingsRepository repository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly IUserRepository userRepository;
    private readonly IEvaluationRepository evaluationRepository;

    public UpdateEvaluatorSettingsCommandHandler(
        IEvaluatorSettingsRepository repository,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        IEvaluationRepository evaluationRepository)
    {
        this.repository = repository;
        this.employeeRepository = employeeRepository;
        this.userRepository = userRepository;
        this.evaluationRepository = evaluationRepository;
    }

    public async Task<Result<EvaluatorSettingsResponse>> Handle(UpdateEvaluatorSettingsCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindByEmployeeIdForUpdateAsync(request.EmployeeId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EvaluatorSettingsResponse>(ErrorCodes.EvaluatorSettingsNotFound);
        }

        var version = EditVersion.Claim(entity, request.Version);
        if (version.IsFailure)
        {
            return Result.Failure<EvaluatorSettingsResponse>(version.Error);
        }

        var controllerCheck = await Services.ControllerRoleCheck.EnsureCanControlAsync(
            request.EmployeeId,
            request.ControllerEmployeeId,
            this.employeeRepository,
            this.userRepository,
            cancellationToken);
        if (controllerCheck.IsFailure)
        {
            return Result.Failure<EvaluatorSettingsResponse>(controllerCheck.Error);
        }

        if (entity.ControllerEmployeeId != request.ControllerEmployeeId)
        {
            // Everything of this evaluator that still waits for a controller goes to the new one; the previous
            // controller no longer supervises the evaluator.
            var open = await this.evaluationRepository.GetUnapprovedForUpdateByEvaluatorAsync(
                request.EmployeeId,
                cancellationToken);
            foreach (var evaluation in open)
            {
                EvaluationReassignment.Assign(evaluation, evaluation.EvaluatorEmployeeId, request.ControllerEmployeeId);
            }
        }

        entity.ControllerEmployeeId = request.ControllerEmployeeId;
        entity.UpdatedAt = DateTime.UtcNow;

        await this.repository.SaveChangesAsync(cancellationToken);

        var updated = await this.repository.FindByEmployeeIdAsync(request.EmployeeId, cancellationToken);
        return Result.Success(HrMappings.ToResponse(updated!));
    }
}

public sealed record LinkEmployeeUserCommand(long EmployeeId, long? UserId, int? Version) : IRequest<Result<EmployeeResponse>>;

public sealed class LinkEmployeeUserCommandHandler : IRequestHandler<LinkEmployeeUserCommand, Result<EmployeeResponse>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly IUserRepository userRepository;
    private readonly ICurrentUserService currentUserService;

    public LinkEmployeeUserCommandHandler(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.userRepository = userRepository;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<EmployeeResponse>> Handle(LinkEmployeeUserCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.employeeRepository.FindByIdForUpdateAsync(request.EmployeeId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeNotFound);
        }

        var version = EditVersion.Claim(entity, request.Version);
        if (version.IsFailure)
        {
            return Result.Failure<EmployeeResponse>(version.Error);
        }

        if (request.UserId is not null)
        {
            var user = await this.userRepository.FindByIdWithRolesAsync(request.UserId.Value, cancellationToken);
            if (user is null || !user.IsActive)
            {
                return Result.Failure<EmployeeResponse>(ErrorCodes.UserNotFound);
            }

            if (await this.employeeRepository.IsUserLinkedToAnotherEmployeeAsync(request.UserId.Value, request.EmployeeId, cancellationToken))
            {
                return Result.Failure<EmployeeResponse>(ErrorCodes.UserAlreadyLinked);
            }
        }

        entity.UserId = request.UserId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;

        await this.employeeRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.employeeRepository.FindByIdAsync(request.EmployeeId, cancellationToken);
        return Result.Success(HrMappings.ToResponse(updated!));
    }
}
