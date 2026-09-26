using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common.Models;
using VariableCompensation.Application.Common;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Application.Hr.EvaluatorSettings.Services;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Hr.Employees.Commands;

public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    long OrganizationUnitId,
    long JobPositionId,
    long? EducationLevelId,
    long? EvaluatorEmployeeId,
    DateOnly? HiredAt) : IRequest<Result<EmployeeResponse>>;

public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<EmployeeResponse>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly IOrganizationUnitRepository organizationUnitRepository;
    private readonly IJobPositionRepository jobPositionRepository;
    private readonly IEducationLevelRepository educationLevelRepository;
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository;
    private readonly IUserRepository userRepository;
    private readonly ICurrentUserService currentUserService;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IOrganizationUnitRepository organizationUnitRepository,
        IJobPositionRepository jobPositionRepository,
        IEducationLevelRepository educationLevelRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.organizationUnitRepository = organizationUnitRepository;
        this.jobPositionRepository = jobPositionRepository;
        this.educationLevelRepository = educationLevelRepository;
        this.evaluatorSettingsRepository = evaluatorSettingsRepository;
        this.userRepository = userRepository;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<EmployeeResponse>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var validation = await this.ValidateReferencesAsync(request, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<EmployeeResponse>(validation.Error);
        }

        var entity = new Employee
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            OrganizationUnitId = request.OrganizationUnitId,
            JobPositionId = request.JobPositionId,
            EducationLevelId = request.EducationLevelId,
            EvaluatorEmployeeId = request.EvaluatorEmployeeId,
            HiredAt = request.HiredAt,
            IsActive = true,
            CreatedByUserId = this.currentUserService.UserId,
            UpdatedByUserId = this.currentUserService.UserId
        };

        await this.employeeRepository.AddAsync(entity, cancellationToken);
        await this.employeeRepository.SaveChangesAsync(cancellationToken);

        var created = await this.employeeRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(HrMappings.ToResponse(created!));
    }

    private async Task<Result> ValidateReferencesAsync(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Result.Failure(ErrorCodes.FirstAndLastNameRequired);
        }

        if (await this.organizationUnitRepository.FindByIdAsync(request.OrganizationUnitId, cancellationToken) is null)
        {
            return Result.Failure(ErrorCodes.OrganizationUnitNotFound);
        }

        if (await this.jobPositionRepository.FindByIdAsync(request.JobPositionId, cancellationToken) is null)
        {
            return Result.Failure(ErrorCodes.JobPositionNotFound);
        }

        if (request.EducationLevelId is null or <= 0)
        {
            return Result.Failure(ErrorCodes.EducationLevelRequired);
        }

        if (await this.educationLevelRepository.FindByIdAsync(request.EducationLevelId.Value, cancellationToken) is null)
        {
            return Result.Failure(ErrorCodes.EducationLevelNotFound);
        }

        if (request.EvaluatorEmployeeId is not null)
        {
            if (!await this.employeeRepository.ExistsAsync(request.EvaluatorEmployeeId.Value, cancellationToken))
            {
                return Result.Failure(ErrorCodes.EvaluatorNotFound);
            }

            if (!await this.evaluatorSettingsRepository.ExistsAsync(request.EvaluatorEmployeeId.Value, cancellationToken))
            {
                return Result.Failure(ErrorCodes.EvaluatorNotConfigured);
            }

            if (!await ActiveAssignee.CanSignInAsync(
                    request.EvaluatorEmployeeId.Value,
                    this.employeeRepository,
                    this.userRepository,
                    cancellationToken))
            {
                return Result.Failure(ErrorCodes.EvaluatorInactive);
            }
        }

        return Result.Success();
    }
}

public sealed record UpdateEmployeeCommand(
    long Id,
    string FirstName,
    string LastName,
    long OrganizationUnitId,
    long JobPositionId,
    long? EducationLevelId,
    long? EvaluatorEmployeeId,
    DateOnly? HiredAt,
    bool IsActive,
    int? Version) : IRequest<Result<EmployeeResponse>>;

public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result<EmployeeResponse>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly IOrganizationUnitRepository organizationUnitRepository;
    private readonly IJobPositionRepository jobPositionRepository;
    private readonly IEducationLevelRepository educationLevelRepository;
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository;
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IUserRepository userRepository;
    private readonly ICurrentUserService currentUserService;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IOrganizationUnitRepository organizationUnitRepository,
        IJobPositionRepository jobPositionRepository,
        IEducationLevelRepository educationLevelRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        IEvaluationRepository evaluationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.organizationUnitRepository = organizationUnitRepository;
        this.jobPositionRepository = jobPositionRepository;
        this.educationLevelRepository = educationLevelRepository;
        this.evaluatorSettingsRepository = evaluatorSettingsRepository;
        this.evaluationRepository = evaluationRepository;
        this.userRepository = userRepository;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<EmployeeResponse>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.employeeRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeNotFound);
        }

        var version = EditVersion.Claim(entity, request.Version);
        if (version.IsFailure)
        {
            return Result.Failure<EmployeeResponse>(version.Error);
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.FirstAndLastNameRequired);
        }

        if (await this.organizationUnitRepository.FindByIdAsync(request.OrganizationUnitId, cancellationToken) is null)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.OrganizationUnitNotFound);
        }

        if (await this.jobPositionRepository.FindByIdAsync(request.JobPositionId, cancellationToken) is null)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.JobPositionNotFound);
        }

        if (request.EducationLevelId is null or <= 0)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EducationLevelRequired);
        }

        if (await this.educationLevelRepository.FindByIdAsync(request.EducationLevelId.Value, cancellationToken) is null)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EducationLevelNotFound);
        }

        if (request.EvaluatorEmployeeId == request.Id)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeSelfEvaluator);
        }

        // Deactivating an evaluator would leave the people they rate without
        // anyone able to rate them, the same hole the role rules close.
        var deactivating = entity.IsActive && !request.IsActive;
        if (deactivating && await this.employeeRepository.HasSubordinatesAsync(request.Id, cancellationToken))
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeHasSubordinates);
        }

        // Their account goes with them, and a controller without one leaves their evaluators' reviews to nobody.
        if (deactivating && await this.evaluatorSettingsRepository.IsControllerForAnyEvaluatorAsync(request.Id, cancellationToken))
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeControlsEvaluators);
        }

        if (request.EvaluatorEmployeeId is not null)
        {
            if (!await this.employeeRepository.ExistsAsync(request.EvaluatorEmployeeId.Value, cancellationToken))
            {
                return Result.Failure<EmployeeResponse>(ErrorCodes.EvaluatorNotFound);
            }

            if (!await this.evaluatorSettingsRepository.ExistsAsync(request.EvaluatorEmployeeId.Value, cancellationToken))
            {
                return Result.Failure<EmployeeResponse>(ErrorCodes.EvaluatorNotConfigured);
            }

            // Only a new choice is checked: an evaluator who has left since is replaced, not refused on every save.
            if (entity.EvaluatorEmployeeId != request.EvaluatorEmployeeId
                && !await ActiveAssignee.CanSignInAsync(
                    request.EvaluatorEmployeeId.Value,
                    this.employeeRepository,
                    this.userRepository,
                    cancellationToken))
            {
                return Result.Failure<EmployeeResponse>(ErrorCodes.EvaluatorInactive);
            }
        }

        var evaluatorChanged = entity.EvaluatorEmployeeId != request.EvaluatorEmployeeId;

        // An evaluation that is not approved yet still needs someone to rate it, or to take it back when the
        // controller returns it for revision.
        if (evaluatorChanged && request.EvaluatorEmployeeId is null
            && await this.evaluationRepository.HasUnapprovedForEmployeeAsync(request.Id, cancellationToken))
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeHasOpenEvaluations);
        }

        if (evaluatorChanged && request.EvaluatorEmployeeId is { } newEvaluatorId)
        {
            var controllerId = await this.evaluationRepository.GetControllerEmployeeIdAsync(newEvaluatorId, cancellationToken);

            // The employee reviews this evaluator, so they would end up reviewing their own evaluations.
            if (controllerId == request.Id)
            {
                return Result.Failure<EmployeeResponse>(ErrorCodes.ControllerRatedByEvaluator);
            }

            var drafts = await this.evaluationRepository.GetForUpdateByEmployeeAsync(
                request.Id,
                EvaluationStatus.Draft,
                cancellationToken);
            foreach (var draft in drafts)
            {
                EvaluationReassignment.Assign(draft, newEvaluatorId, controllerId);
            }
        }

        if (deactivating && entity.UserId is { } userId)
        {
            var accountClosed = await this.CloseAccountAsync(userId, cancellationToken);
            if (accountClosed.IsFailure)
            {
                return Result.Failure<EmployeeResponse>(accountClosed.Error);
            }
        }

        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.OrganizationUnitId = request.OrganizationUnitId;
        entity.JobPositionId = request.JobPositionId;
        entity.EducationLevelId = request.EducationLevelId;
        entity.EvaluatorEmployeeId = request.EvaluatorEmployeeId;
        entity.HiredAt = request.HiredAt;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;

        await this.employeeRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.employeeRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(HrMappings.ToResponse(updated!));
    }

    /// <summary>
    /// Someone who has left signs in no more: the account is deactivated and every session of it ends. Reactivating
    /// the employee does not bring the account back; that is decided on the account itself.
    /// </summary>
    private async Task<Result> CloseAccountAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await this.userRepository.FindByIdForUpdateAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Success();
        }

        if (user.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Admin)
            && !await this.userRepository.HasOtherActiveUserInRoleAsync(RoleCodes.Admin, user.Id, cancellationToken))
        {
            return Result.Failure(ErrorCodes.LastActiveAdministrator);
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        // A form still open on the account has not seen it close.
        user.Version++;
        await this.userRepository.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        return Result.Success();
    }
}
