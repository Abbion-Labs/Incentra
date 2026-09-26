using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;

namespace VariableCompensation.Application.Evaluation.Commands;

public sealed record SaveEvaluationPlanningDraftCommand(
    long Id,
    int Version,
    DateTime? ConversationAt,
    string? EvaluatorComment,
    string? ConditionsNotMetComment,
    bool ConditionsFulfilled,
    IReadOnlyList<EvaluationGoalItem> Goals,
    IReadOnlyList<EvaluationConditionItem> Conditions,
    IReadOnlyList<EvaluationCriterionItem> Criteria) : IRequest<Result<EvaluationDetailResponse>>;

public sealed class SaveEvaluationPlanningDraftCommandHandler
    : IRequestHandler<SaveEvaluationPlanningDraftCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IEvaluationLookupRepository lookupRepository;
    private readonly EvaluationScoringService scoringService;
    private readonly ICurrentUserService currentUserService;
    private readonly EvaluationAccessService evaluationAccessService;

    public SaveEvaluationPlanningDraftCommandHandler(
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

    public async Task<Result<EvaluationDetailResponse>> Handle(
        SaveEvaluationPlanningDraftCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        var validation = await EvaluationCommandHelpers.EnsureDraftEditableAsync(
            entity, request.Version, this.evaluationAccessService, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(validation.Error);
        }

        entity = validation.Value;

        // A plan is set as a whole: goals, conditions and criteria together.
        if (request.Goals.Count == 0 || request.Conditions.Count == 0 || request.Criteria.Count == 0)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.PlanIncomplete);
        }

        var planAgreed = EvaluationPlanningRules.IsGoalsPlanningComplete(entity);
        EvaluationDraftMutator.ApplyHeader(
            entity,
            request.ConversationAt,
            request.EvaluatorComment,
            request.ConditionsNotMetComment,
            request.ConditionsFulfilled);

        var goalsResult = await EvaluationDraftMutator.ApplyGoalsAsync(
            entity, request.Goals, this.lookupRepository, cancellationToken, planAgreed);
        if (goalsResult.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(goalsResult.Error);
        }

        var conditionsResult = EvaluationDraftMutator.ApplyConditions(entity, request.Conditions, planAgreed);
        if (conditionsResult.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(conditionsResult.Error);
        }

        var criteriaResult = EvaluationDraftMutator.ApplyCriteria(entity, request.Criteria, planAgreed);
        if (criteriaResult.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(criteriaResult.Error);
        }

        var ratingLevels = await this.lookupRepository.GetRatingLevelsAsync(cancellationToken);
        var descriptiveRatings = await this.lookupRepository.GetDescriptiveRatingsAsync(cancellationToken);
        this.scoringService.Recalculate(entity, ratingLevels, descriptiveRatings);

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;
        entity.Version++;

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}

public sealed record EvaluationTrainingDraftItem(
    string? TrainingDescription,
    string? KnowledgeDescription,
    string? DevelopmentDescription,
    string? EvaluatorComment);

public sealed record SaveEvaluationRatingDraftCommand(
    long Id,
    int Version,
    DateTime? ConversationAt,
    string? EvaluatorComment,
    string? ConditionsNotMetComment,
    bool ConditionsFulfilled,
    IReadOnlyList<EvaluationGoalItem>? Goals,
    IReadOnlyList<EvaluationMeasureItem>? Measures,
    EvaluationTrainingDraftItem? Training) : IRequest<Result<EvaluationDetailResponse>>;

public sealed class SaveEvaluationRatingDraftCommandHandler
    : IRequestHandler<SaveEvaluationRatingDraftCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IEvaluationLookupRepository lookupRepository;
    private readonly EvaluationScoringService scoringService;
    private readonly ICurrentUserService currentUserService;
    private readonly EvaluationAccessService evaluationAccessService;

    public SaveEvaluationRatingDraftCommandHandler(
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

    public async Task<Result<EvaluationDetailResponse>> Handle(
        SaveEvaluationRatingDraftCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        var validation = await EvaluationCommandHelpers.EnsureDraftEditableAsync(
            entity, request.Version, this.evaluationAccessService, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(validation.Error);
        }

        entity = validation.Value;
        EvaluationDraftMutator.ApplyHeader(
            entity,
            request.ConversationAt,
            request.EvaluatorComment,
            request.ConditionsNotMetComment,
            request.ConditionsFulfilled);

        if (request.ConditionsFulfilled)
        {
            // Left out means unchanged: an absent list must never wipe what is saved.
            if (request.Goals is not null)
            {
                var goalsResult = await EvaluationDraftMutator.ApplyGoalsAsync(
                    entity, request.Goals, this.lookupRepository, cancellationToken);
                if (goalsResult.IsFailure)
                {
                    return Result.Failure<EvaluationDetailResponse>(goalsResult.Error);
                }
            }

            if (request.Measures is not null)
            {
                var measuresResult = await EvaluationDraftMutator.ApplyMeasuresAsync(
                    entity, request.Measures, this.lookupRepository, cancellationToken);
                if (measuresResult.IsFailure)
                {
                    return Result.Failure<EvaluationDetailResponse>(measuresResult.Error);
                }
            }

            if (request.Training is not null)
            {
                EvaluationDraftMutator.ApplyTraining(
                    entity,
                    request.Training.TrainingDescription,
                    request.Training.KnowledgeDescription,
                    request.Training.DevelopmentDescription,
                    request.Training.EvaluatorComment);
            }
        }

        var ratingLevels = await this.lookupRepository.GetRatingLevelsAsync(cancellationToken);
        var descriptiveRatings = await this.lookupRepository.GetDescriptiveRatingsAsync(cancellationToken);
        this.scoringService.Recalculate(entity, ratingLevels, descriptiveRatings);

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;
        entity.Version++;

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}
