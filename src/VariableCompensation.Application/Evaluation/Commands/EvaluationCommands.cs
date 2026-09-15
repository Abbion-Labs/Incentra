using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Evaluation.Commands;

public sealed record CreateEvaluationCommand(long EmployeeId, short Year, byte Quarter) : IRequest<Result<EvaluationDetailResponse>>;

public sealed class CreateEvaluationCommandHandler : IRequestHandler<CreateEvaluationCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly EvaluationAccessService evaluationAccessService;

    public CreateEvaluationCommandHandler(
        IEvaluationRepository evaluationRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        EvaluationAccessService evaluationAccessService)
    {
        this.evaluationRepository = evaluationRepository;
        this.employeeRepository = employeeRepository;
        this.currentUserService = currentUserService;
        this.evaluationAccessService = evaluationAccessService;
    }

    public async Task<Result<EvaluationDetailResponse>> Handle(CreateEvaluationCommand request, CancellationToken cancellationToken)
    {
        if (request.Quarter is < 1 or > 4)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.QuarterInvalid);
        }

        var employee = await this.employeeRepository.FindByIdWithEvaluatorAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EmployeeNotFound);
        }

        if (!employee.IsActive)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EmployeeInactive);
        }

        if (employee.EvaluatorEmployeeId is null)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EmployeeMissingEvaluator);
        }

        var access = await this.evaluationAccessService.EnsureCanCreateForEmployeeAsync(
            employee.EvaluatorEmployeeId.Value,
            cancellationToken);
        if (access.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(access.Error);
        }

        if (await this.evaluationRepository.ExistsForEmployeeQuarterAsync(
                request.EmployeeId, request.Year, request.Quarter, null, cancellationToken))
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EvaluationAlreadyExists);
        }

        var controllerId = await this.evaluationRepository.GetControllerEmployeeIdAsync(
            employee.EvaluatorEmployeeId.Value,
            cancellationToken);

        var entity = new EvaluationEntity
        {
            EmployeeId = request.EmployeeId,
            EvaluatorEmployeeId = employee.EvaluatorEmployeeId.Value,
            ControllerEmployeeId = controllerId,
            Year = request.Year,
            Quarter = request.Quarter,
            Status = EvaluationStatus.Draft,
            CreatedByUserId = this.currentUserService.UserId,
            UpdatedByUserId = this.currentUserService.UserId
        };

        await this.evaluationRepository.AddAsync(entity, cancellationToken);
        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        var created = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(created!));
    }
}

public sealed record UpdateEvaluationDraftCommand(
    long Id,
    int Version,
    DateTime? ConversationAt,
    string? EvaluatorComment,
    string? ConditionsNotMetComment,
    bool ConditionsFulfilled) : IRequest<Result<EvaluationDetailResponse>>;

public sealed class UpdateEvaluationDraftCommandHandler : IRequestHandler<UpdateEvaluationDraftCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IEvaluationLookupRepository lookupRepository;
    private readonly EvaluationScoringService scoringService;
    private readonly ICurrentUserService currentUserService;
    private readonly EvaluationAccessService evaluationAccessService;

    public UpdateEvaluationDraftCommandHandler(
        IEvaluationRepository evaluationRepository,
        IEvaluationLookupRepository lookupRepository,
        EvaluationScoringService scoringService,
        ICurrentUserService currentUserService,
        EvaluationAccessService evaluationAccessService)
    {
        this.evaluationRepository = evaluationRepository;
        this.lookupRepository = lookupRepository;
        this.scoringService = scoringService;
        this.currentUserService = currentUserService;
        this.evaluationAccessService = evaluationAccessService;
    }

    public async Task<Result<EvaluationDetailResponse>> Handle(UpdateEvaluationDraftCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        var validation = await EvaluationCommandHelpers.EnsureDraftEditableAsync(
            entity, request.Version, this.evaluationAccessService, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(validation.Error);
        }

        entity = validation.Value;
        entity.ConversationAt = request.ConversationAt;
        entity.EvaluatorComment = request.EvaluatorComment;
        entity.ConditionsNotMetComment = request.ConditionsNotMetComment;
        entity.ConditionsFulfilled = request.ConditionsFulfilled;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;

        var ratingLevels = await this.lookupRepository.GetRatingLevelsAsync(cancellationToken);
        var descriptiveRatings = await this.lookupRepository.GetDescriptiveRatingsAsync(cancellationToken);
        this.scoringService.Recalculate(entity, ratingLevels, descriptiveRatings);

        entity.Version++;

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}

internal static class EvaluationCommandHelpers
{
    public static Result<EvaluationEntity> EnsureDraft(EvaluationEntity? entity, int version)
    {
        if (entity is null)
        {
            return Result.Failure<EvaluationEntity>(ErrorCodes.EvaluationNotFound);
        }

        if (entity.Version != version)
        {
            return Result.Failure<EvaluationEntity>(ErrorCodes.EvaluationVersionConflict);
        }

        if (!EvaluationWorkflow.CanEdit(entity.Status))
        {
            return Result.Failure<EvaluationEntity>(ErrorCodes.DraftOnlyEditable);
        }

        return Result.Success(entity);
    }

    public static async Task<Result<EvaluationEntity>> EnsureDraftEditableAsync(
        EvaluationEntity? entity,
        int version,
        EvaluationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var draft = EnsureDraft(entity, version);
        if (draft.IsFailure)
        {
            return draft;
        }

        var access = await accessService.EnsureCanEditDraftAsync(draft.Value, cancellationToken);
        return access.IsFailure ? Result.Failure<EvaluationEntity>(access.Error) : draft;
    }
}
