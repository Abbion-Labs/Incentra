using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common.Models;
using VariableCompensation.Application.Evaluation.Services;
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
    private readonly ICurrentUserService currentUserService;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IOrganizationUnitRepository organizationUnitRepository,
        IJobPositionRepository jobPositionRepository,
        IEducationLevelRepository educationLevelRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.organizationUnitRepository = organizationUnitRepository;
        this.jobPositionRepository = jobPositionRepository;
        this.educationLevelRepository = educationLevelRepository;
        this.evaluatorSettingsRepository = evaluatorSettingsRepository;
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
    bool IsActive) : IRequest<Result<EmployeeResponse>>;

public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result<EmployeeResponse>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly IOrganizationUnitRepository organizationUnitRepository;
    private readonly IJobPositionRepository jobPositionRepository;
    private readonly IEducationLevelRepository educationLevelRepository;
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository;
    private readonly IEvaluationRepository evaluationRepository;
    private readonly ICurrentUserService currentUserService;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IOrganizationUnitRepository organizationUnitRepository,
        IJobPositionRepository jobPositionRepository,
        IEducationLevelRepository educationLevelRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        IEvaluationRepository evaluationRepository,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.organizationUnitRepository = organizationUnitRepository;
        this.jobPositionRepository = jobPositionRepository;
        this.educationLevelRepository = educationLevelRepository;
        this.evaluatorSettingsRepository = evaluatorSettingsRepository;
        this.evaluationRepository = evaluationRepository;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<EmployeeResponse>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.employeeRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeNotFound);
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
        if (entity.IsActive && !request.IsActive
            && await this.employeeRepository.HasSubordinatesAsync(request.Id, cancellationToken))
        {
            return Result.Failure<EmployeeResponse>(ErrorCodes.EmployeeHasSubordinates);
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
}
