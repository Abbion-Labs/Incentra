using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth;
using VariableCompensation.Application.Common;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Application.Hr.Models;
using EvaluatorSettingsEntity = VariableCompensation.Domain.Entities.Hr.EvaluatorSettings;
using VariableCompensation.Domain.Enums;
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

        if (entity.ControllerEmployeeId != request.ControllerEmployeeId)
        {
            // Only a new choice is checked: the controller in place was checked when chosen.
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

            // Everything of this evaluator that still waits for a controller goes to the new one; the previous
            // controller no longer supervises the evaluator.
            var open = await this.evaluationRepository.GetUnapprovedForUpdateByEvaluatorAsync(
                request.EmployeeId,
                cancellationToken);

            // Without a controller an evaluation is approved when it is submitted. One submitted before that would
            // be left waiting for a review nobody can give, so the controller stays until those are decided.
            if (request.ControllerEmployeeId is null
                && open.Any(e => e.Status is EvaluationStatus.Submitted or EvaluationStatus.UnderReview))
            {
                return Result.Failure<EvaluatorSettingsResponse>(ErrorCodes.EvaluatorHasPendingReviews);
            }

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
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository;
    private readonly ICurrentUserService currentUserService;

    public LinkEmployeeUserCommandHandler(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.userRepository = userRepository;
        this.evaluatorSettingsRepository = evaluatorSettingsRepository;
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

        var linkCheck = request.UserId == entity.UserId
            ? Result.Success()
            : entity.UserId is not null
                ? await this.EnsureCanUnlinkAsync(entity.UserId.Value, request.UserId, cancellationToken)
                : await this.EnsureCanLinkAsync(entity, request.UserId!.Value, cancellationToken);
        if (linkCheck.IsFailure)
        {
            return Result.Failure<EmployeeResponse>(linkCheck.Error);
        }

        entity.UserId = request.UserId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;

        await this.employeeRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.employeeRepository.FindByIdAsync(request.EmployeeId, cancellationToken);
        return Result.Success(HrMappings.ToResponse(updated!));
    }

    /// <summary>
    /// An account is one person's sign-in, so it is never handed over to someone else: the link is only undone,
    /// to correct a mistake. And only for an account without the roles that act as an employee; the rules for
    /// removing those roles already see to it that nobody is left without an evaluator or a controller.
    /// </summary>
    private async Task<Result> EnsureCanUnlinkAsync(long currentUserId, long? replacementUserId, CancellationToken cancellationToken)
    {
        if (replacementUserId is not null)
        {
            return Result.Failure(ErrorCodes.EmployeeAccountChangeRequiresUnlink);
        }

        var current = await this.userRepository.FindByIdWithRolesAsync(currentUserId, cancellationToken);
        var roles = current?.UserRoles.Select(ur => ur.Role.Code).ToList() ?? [];
        return EmployeeLinkedRoles.AnyIn(roles)
            ? Result.Failure(ErrorCodes.AccountRolesRequireEmployee)
            : Result.Success();
    }

    private async Task<Result> EnsureCanLinkAsync(Employee employee, long userId, CancellationToken cancellationToken)
    {
        if (!employee.IsActive)
        {
            return Result.Failure(ErrorCodes.EmployeeInactive);
        }

        var user = await this.userRepository.FindByIdWithRolesAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure(ErrorCodes.UserNotFound);
        }

        if (await this.employeeRepository.IsUserLinkedToAnotherEmployeeAsync(userId, employee.Id, cancellationToken))
        {
            return Result.Failure(ErrorCodes.UserAlreadyLinked);
        }

        // The evaluator role and the evaluator settings come and go together.
        var isEvaluator = user.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Evaluator);
        var hasSettings = await this.evaluatorSettingsRepository.ExistsAsync(employee.Id, cancellationToken);
        return isEvaluator == hasSettings
            ? Result.Success()
            : Result.Failure(ErrorCodes.EvaluatorRoleSettingsMismatch);
    }
}
