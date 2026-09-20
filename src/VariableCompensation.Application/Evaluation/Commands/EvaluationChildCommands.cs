using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Evaluation;

namespace VariableCompensation.Application.Evaluation.Commands;

public sealed record EvaluationGoalItem(
    string Description,
    long? RatingLevelId,
    string? Comment,
    decimal? Weight,
    int SortOrder);

public sealed record ReplaceEvaluationGoalsCommand(long Id, int Version, IReadOnlyList<EvaluationGoalItem> Goals)
    : IRequest<Result<EvaluationDetailResponse>>;

public sealed class ReplaceEvaluationGoalsCommandHandler : IRequestHandler<ReplaceEvaluationGoalsCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IEvaluationLookupRepository lookupRepository;
    private readonly EvaluationScoringService scoringService;
    private readonly ICurrentUserService currentUserService;

    private readonly EvaluationAccessService evaluationAccessService;

    public ReplaceEvaluationGoalsCommandHandler(
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

    public async Task<Result<EvaluationDetailResponse>> Handle(ReplaceEvaluationGoalsCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        var validation = await EvaluationCommandHelpers.EnsureDraftEditableAsync(entity, request.Version, this.evaluationAccessService, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(validation.Error);
        }

        entity = validation.Value;

        var planningLock = EvaluationPlanningRules.EnsureGoalsPlanningEditable(entity!, request.Goals);
        if (planningLock.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(planningLock.Error);
        }

        var ratingLevels = await this.lookupRepository.GetRatingLevelsAsync(cancellationToken);
        var notRatedLevel = ratingLevels.FirstOrDefault(r => r.Value == RatingLevelRules.NotRatedValue);
        if (notRatedLevel is null)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.NotRatedLevelMissing);
        }

        foreach (var item in request.Goals)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return Result.Failure<EvaluationDetailResponse>(ErrorCodes.GoalDescriptionRequired);
            }

            if (item.RatingLevelId is not null &&
                !await this.lookupRepository.RatingLevelExistsAsync(item.RatingLevelId.Value, cancellationToken))
            {
                return Result.Failure<EvaluationDetailResponse>($"{ErrorCodes.RatingLevelNotFound}?id={item.RatingLevelId}");
            }
        }

        entity!.Goals.Clear();
        foreach (var item in request.Goals.OrderBy(g => g.SortOrder))
        {
            entity.Goals.Add(new EvaluationGoal
            {
                Description = item.Description.Trim(),
                RatingLevelId = item.RatingLevelId ?? notRatedLevel.Id,
                Comment = item.Comment,
                Weight = item.Weight,
                SortOrder = item.SortOrder
            });
        }

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

public sealed record EvaluationMeasureItem(
    long MeasureTypeId,
    string? RatingComment,
    long RatingLevelId,
    int SortOrder);

public sealed record ReplaceEvaluationMeasuresCommand(long Id, int Version, IReadOnlyList<EvaluationMeasureItem> Measures)
    : IRequest<Result<EvaluationDetailResponse>>;

public sealed class ReplaceEvaluationMeasuresCommandHandler : IRequestHandler<ReplaceEvaluationMeasuresCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IEvaluationLookupRepository lookupRepository;
    private readonly EvaluationScoringService scoringService;
    private readonly ICurrentUserService currentUserService;

    private readonly EvaluationAccessService evaluationAccessService;

    public ReplaceEvaluationMeasuresCommandHandler(
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

    public async Task<Result<EvaluationDetailResponse>> Handle(ReplaceEvaluationMeasuresCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        var validation = await EvaluationCommandHelpers.EnsureDraftEditableAsync(entity, request.Version, this.evaluationAccessService, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(validation.Error);
        }

        entity = validation.Value;

        foreach (var item in request.Measures)
        {
            if (!await this.lookupRepository.MeasureTypeExistsAsync(item.MeasureTypeId, cancellationToken))
            {
                return Result.Failure<EvaluationDetailResponse>($"{ErrorCodes.MeasureTypeNotFound}?id={item.MeasureTypeId}");
            }

            if (!await this.lookupRepository.RatingLevelExistsAsync(item.RatingLevelId, cancellationToken))
            {
                return Result.Failure<EvaluationDetailResponse>($"{ErrorCodes.RatingLevelNotFound}?id={item.RatingLevelId}");
            }
        }

        entity!.Measures.Clear();
        foreach (var item in request.Measures.OrderBy(m => m.SortOrder))
        {
            entity.Measures.Add(new EvaluationMeasure
            {
                MeasureTypeId = item.MeasureTypeId,
                RatingComment = item.RatingComment,
                RatingLevelId = item.RatingLevelId,
                SortOrder = item.SortOrder
            });
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

public sealed record EvaluationCriterionItem(string Description, int SortOrder);

public sealed record ReplaceEvaluationCriteriaCommand(long Id, int Version, IReadOnlyList<EvaluationCriterionItem> Criteria)
    : IRequest<Result<EvaluationDetailResponse>>;

public sealed class ReplaceEvaluationCriteriaCommandHandler : IRequestHandler<ReplaceEvaluationCriteriaCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly ICurrentUserService currentUserService;

    private readonly EvaluationAccessService evaluationAccessService;

    public ReplaceEvaluationCriteriaCommandHandler(
        IEvaluationRepository evaluationRepository,
        ICurrentUserService currentUserService,
        EvaluationAccessService evaluationAccessService)
    {
        this.evaluationRepository = evaluationRepository;
        this.currentUserService = currentUserService;
        this.evaluationAccessService = evaluationAccessService;
    }

    public async Task<Result<EvaluationDetailResponse>> Handle(ReplaceEvaluationCriteriaCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        var validation = await EvaluationCommandHelpers.EnsureDraftEditableAsync(entity, request.Version, this.evaluationAccessService, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(validation.Error);
        }

        entity = validation.Value;

        var planningLock = EvaluationPlanningRules.EnsureCriteriaPlanningEditable(entity!);
        if (planningLock.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(planningLock.Error);
        }

        entity.Criteria.Clear();
        foreach (var item in request.Criteria.OrderBy(c => c.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return Result.Failure<EvaluationDetailResponse>(ErrorCodes.CriterionDescriptionRequired);
            }

            entity.Criteria.Add(new EvaluationCriterion
            {
                Description = item.Description.Trim(),
                SortOrder = item.SortOrder
            });
        }

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;
        entity.Version++;

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}

public sealed record EvaluationConditionItem(string Description, int SortOrder);

public sealed record ReplaceEvaluationConditionsCommand(long Id, int Version, IReadOnlyList<EvaluationConditionItem> Conditions)
    : IRequest<Result<EvaluationDetailResponse>>;

public sealed class ReplaceEvaluationConditionsCommandHandler : IRequestHandler<ReplaceEvaluationConditionsCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly ICurrentUserService currentUserService;

    private readonly EvaluationAccessService evaluationAccessService;

    public ReplaceEvaluationConditionsCommandHandler(
        IEvaluationRepository evaluationRepository,
        ICurrentUserService currentUserService,
        EvaluationAccessService evaluationAccessService)
    {
        this.evaluationRepository = evaluationRepository;
        this.currentUserService = currentUserService;
        this.evaluationAccessService = evaluationAccessService;
    }

    public async Task<Result<EvaluationDetailResponse>> Handle(ReplaceEvaluationConditionsCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        var validation = await EvaluationCommandHelpers.EnsureDraftEditableAsync(entity, request.Version, this.evaluationAccessService, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(validation.Error);
        }

        entity = validation.Value;

        var planningLock = EvaluationPlanningRules.EnsureConditionsPlanningEditable(entity!);
        if (planningLock.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(planningLock.Error);
        }

        entity.Conditions.Clear();
        foreach (var item in request.Conditions.OrderBy(c => c.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return Result.Failure<EvaluationDetailResponse>(ErrorCodes.ConditionDescriptionRequired);
            }

            entity.Conditions.Add(new EvaluationCondition
            {
                Description = item.Description.Trim(),
                SortOrder = item.SortOrder
            });
        }

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;
        entity.Version++;

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}

public sealed record UpsertEvaluationTrainingCommand(
    long Id,
    int Version,
    string? TrainingDescription,
    string? KnowledgeDescription,
    string? DevelopmentDescription,
    string? EvaluatorComment) : IRequest<Result<EvaluationDetailResponse>>;

public sealed class UpsertEvaluationTrainingCommandHandler : IRequestHandler<UpsertEvaluationTrainingCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly ICurrentUserService currentUserService;

    private readonly EvaluationAccessService evaluationAccessService;

    public UpsertEvaluationTrainingCommandHandler(
        IEvaluationRepository evaluationRepository,
        ICurrentUserService currentUserService,
        EvaluationAccessService evaluationAccessService)
    {
        this.evaluationRepository = evaluationRepository;
        this.currentUserService = currentUserService;
        this.evaluationAccessService = evaluationAccessService;
    }

    public async Task<Result<EvaluationDetailResponse>> Handle(UpsertEvaluationTrainingCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        var validation = await EvaluationCommandHelpers.EnsureDraftEditableAsync(entity, request.Version, this.evaluationAccessService, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(validation.Error);
        }

        entity = validation.Value;
        entity.Training ??= new EvaluationTraining();
        entity.Training.TrainingDescription = request.TrainingDescription;
        entity.Training.KnowledgeDescription = request.KnowledgeDescription;
        entity.Training.DevelopmentDescription = request.DevelopmentDescription;
        entity.Training.EvaluatorComment = request.EvaluatorComment;
        entity.Training.UpdatedAt = DateTime.UtcNow;

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = this.currentUserService.UserId;
        entity.Version++;

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}
