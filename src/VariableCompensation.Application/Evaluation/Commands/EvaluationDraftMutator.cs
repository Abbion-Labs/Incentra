using CSharpFunctionalExtensions;
using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Evaluation;

namespace VariableCompensation.Application.Evaluation.Commands;

internal static class EvaluationDraftMutator
{
    /// <summary>A goal worth less than this would not move the average.</summary>
    public const int MinGoalWeightPercent = 5;

    public static void ApplyHeader(
        EvaluationEntity entity,
        DateTime? conversationAt,
        string? evaluatorComment,
        string? conditionsNotMetComment,
        bool conditionsFulfilled)
    {
        entity.ConversationAt = conversationAt;
        entity.EvaluatorComment = evaluatorComment;
        entity.ConditionsNotMetComment = conditionsNotMetComment;
        entity.ConditionsFulfilled = conditionsFulfilled;
    }

    /// <summary>
    /// Sets the goals while the plan is still being made. Once goals, conditions and criteria are set the plan is
    /// what was agreed with the employee: from then on only the rating and comment of each existing goal change.
    /// </summary>
    public static async Task<Result> ApplyGoalsAsync(
        EvaluationEntity entity,
        IReadOnlyList<EvaluationGoalItem> goals,
        IEvaluationLookupRepository lookupRepository,
        CancellationToken cancellationToken)
    {
        if (EvaluationPlanningRules.IsGoalsPlanningComplete(entity))
        {
            return await RateAgreedGoalsAsync(entity, goals, lookupRepository, cancellationToken);
        }

        var ratingLevels = await lookupRepository.GetRatingLevelsAsync(cancellationToken);
        var notRatedLevel = ratingLevels.FirstOrDefault(r => r.Value == RatingLevelRules.NotRatedValue);
        if (notRatedLevel is null)
        {
            return Result.Failure(ErrorCodes.NotRatedLevelMissing);
        }

        foreach (var item in goals)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return Result.Failure(ErrorCodes.GoalDescriptionRequired);
            }

            if (item.RatingLevelId is not null &&
                !await lookupRepository.RatingLevelExistsAsync(item.RatingLevelId.Value, cancellationToken))
            {
                return Result.Failure($"{ErrorCodes.RatingLevelNotFound}?id={item.RatingLevelId}");
            }
        }

        var weights = ValidateWeights(goals);
        if (weights.IsFailure)
        {
            return weights;
        }

        entity.Goals.Clear();
        foreach (var item in goals.OrderBy(g => g.SortOrder))
        {
            entity.Goals.Add(new EvaluationGoal
            {
                Description = item.Description.Trim(),
                RatingLevelId = item.RatingLevelId ?? notRatedLevel.Id,
                Comment = item.Comment,
                Weight = item.Weight,
                SortOrder = item.SortOrder,
            });
        }

        return Result.Success();
    }

    /// <summary>
    /// The weight of a goal is the share of the goals average it carries, in whole percent. Either every goal has
    /// one and together they make 100, or none has, and the goals count equally.
    /// </summary>
    private static Result ValidateWeights(IReadOnlyList<EvaluationGoalItem> goals)
    {
        if (goals.All(g => g.Weight is null))
        {
            return Result.Success();
        }

        if (goals.Any(g => g.Weight is null))
        {
            return Result.Failure(ErrorCodes.GoalWeightsIncomplete);
        }

        if (goals.Any(g => g.Weight!.Value % 1 != 0
            || g.Weight.Value < MinGoalWeightPercent
            || g.Weight.Value > 100))
        {
            return Result.Failure($"{ErrorCodes.GoalWeightInvalid}?min={MinGoalWeightPercent}");
        }

        var total = goals.Sum(g => g.Weight!.Value);
        return total == 100
            ? Result.Success()
            : Result.Failure($"{ErrorCodes.GoalWeightsSumInvalid}?total={total:0}");
    }

    /// <summary>
    /// Rates the agreed goals. Every goal has to be sent, by its id, with its text and weight unchanged; adding,
    /// removing or rewording a goal is refused.
    /// </summary>
    private static async Task<Result> RateAgreedGoalsAsync(
        EvaluationEntity entity,
        IReadOnlyList<EvaluationGoalItem> goals,
        IEvaluationLookupRepository lookupRepository,
        CancellationToken cancellationToken)
    {
        var agreed = entity.Goals.ToDictionary(g => g.Id);
        var sentIds = goals.Select(g => g.Id).ToList();
        if (goals.Count != agreed.Count
            || sentIds.Any(id => id is null || !agreed.ContainsKey(id.Value))
            || sentIds.Distinct().Count() != sentIds.Count)
        {
            return Result.Failure(ErrorCodes.GoalsPlanningLocked);
        }

        var ratingLevels = await lookupRepository.GetRatingLevelsAsync(cancellationToken);
        var notRatedLevel = ratingLevels.FirstOrDefault(r => r.Value == RatingLevelRules.NotRatedValue);
        if (notRatedLevel is null)
        {
            return Result.Failure(ErrorCodes.NotRatedLevelMissing);
        }

        foreach (var item in goals)
        {
            var goal = agreed[item.Id!.Value];
            if (!string.Equals(item.Description.Trim(), goal.Description, StringComparison.Ordinal)
                || item.Weight != goal.Weight)
            {
                return Result.Failure(ErrorCodes.GoalsPlanningLocked);
            }

            if (item.RatingLevelId is not null &&
                !await lookupRepository.RatingLevelExistsAsync(item.RatingLevelId.Value, cancellationToken))
            {
                return Result.Failure($"{ErrorCodes.RatingLevelNotFound}?id={item.RatingLevelId}");
            }
        }

        foreach (var item in goals)
        {
            var goal = agreed[item.Id!.Value];
            goal.RatingLevelId = item.RatingLevelId ?? notRatedLevel.Id;
            goal.Comment = item.Comment;
        }

        return Result.Success();
    }

    public static Result ApplyConditions(
        EvaluationEntity entity,
        IReadOnlyList<EvaluationConditionItem> conditions)
    {
        var planningLock = EvaluationPlanningRules.EnsureConditionsPlanningEditable(entity);
        if (planningLock.IsFailure)
        {
            return planningLock;
        }

        entity.Conditions.Clear();
        foreach (var item in conditions.OrderBy(c => c.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return Result.Failure(ErrorCodes.ConditionDescriptionRequired);
            }

            entity.Conditions.Add(new EvaluationCondition
            {
                Description = item.Description.Trim(),
                SortOrder = item.SortOrder,
            });
        }

        return Result.Success();
    }

    public static Result ApplyCriteria(
        EvaluationEntity entity,
        IReadOnlyList<EvaluationCriterionItem> criteria)
    {
        var planningLock = EvaluationPlanningRules.EnsureCriteriaPlanningEditable(entity);
        if (planningLock.IsFailure)
        {
            return planningLock;
        }

        entity.Criteria.Clear();
        foreach (var item in criteria.OrderBy(c => c.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return Result.Failure(ErrorCodes.CriterionDescriptionRequired);
            }

            entity.Criteria.Add(new EvaluationCriterion
            {
                Description = item.Description.Trim(),
                SortOrder = item.SortOrder,
            });
        }

        return Result.Success();
    }

    public static async Task<Result> ApplyMeasuresAsync(
        EvaluationEntity entity,
        IReadOnlyList<EvaluationMeasureItem> measures,
        IEvaluationLookupRepository lookupRepository,
        CancellationToken cancellationToken)
    {
        foreach (var item in measures)
        {
            if (!await lookupRepository.MeasureTypeExistsAsync(item.MeasureTypeId, cancellationToken))
            {
                return Result.Failure($"{ErrorCodes.MeasureTypeNotFound}?id={item.MeasureTypeId}");
            }

            if (!await lookupRepository.RatingLevelExistsAsync(item.RatingLevelId, cancellationToken))
            {
                return Result.Failure($"{ErrorCodes.RatingLevelNotFound}?id={item.RatingLevelId}");
            }
        }

        entity.Measures.Clear();
        foreach (var item in measures.OrderBy(m => m.SortOrder))
        {
            entity.Measures.Add(new EvaluationMeasure
            {
                MeasureTypeId = item.MeasureTypeId,
                RatingComment = item.RatingComment,
                RatingLevelId = item.RatingLevelId,
                SortOrder = item.SortOrder,
            });
        }

        return Result.Success();
    }

    public static void ApplyTraining(
        EvaluationEntity entity,
        string? trainingDescription,
        string? knowledgeDescription,
        string? developmentDescription,
        string? evaluatorComment)
    {
        entity.Training ??= new EvaluationTraining();
        entity.Training.TrainingDescription = trainingDescription;
        entity.Training.KnowledgeDescription = knowledgeDescription;
        entity.Training.DevelopmentDescription = developmentDescription;
        entity.Training.EvaluatorComment = evaluatorComment;
        entity.Training.UpdatedAt = DateTime.UtcNow;
    }
}
