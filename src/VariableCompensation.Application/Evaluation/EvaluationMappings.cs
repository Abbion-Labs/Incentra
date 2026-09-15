using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Application.Evaluation;

internal static class EvaluationMappings
{
    public static EvaluationSummaryResponse ToSummary(EvaluationEntity entity) =>
        new()
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeFullName = entity.Employee.FullName,
            OrganizationUnitName = entity.Employee.OrganizationUnit?.Name ?? string.Empty,
            EvaluatorEmployeeId = entity.EvaluatorEmployeeId,
            EvaluatorFullName = entity.Evaluator.FullName,
            ControllerEmployeeId = entity.ControllerEmployeeId,
            ControllerFullName = entity.Controller?.FullName,
            Year = entity.Year,
            Quarter = entity.Quarter,
            Status = entity.Status.ToString(),
            OverallAverage = entity.OverallAverage,
            DescriptiveRatingName = entity.DescriptiveRating?.Name,
            SubmittedAt = entity.SubmittedAt,
            ApprovedAt = entity.ApprovedAt,
            Version = entity.Version,
            GoalCount = entity.Goals.Count,
            GoalsPlanningComplete = EvaluationPlanningRules.IsGoalsPlanningComplete(entity),
            HasIncompleteRatings = !entity.ConditionsFulfilled
                ? false
                : entity.Goals.Any(g => g.RatingLevel?.Value == RatingLevelRules.NotRatedValue)
                || entity.Measures.Any(m => m.RatingLevel?.Value == RatingLevelRules.NotRatedValue)
                || (entity.Goals.Count > 0 && entity.Measures.Count == 0),
            ControllerComment = entity.ControllerComment,
            ControllerViewedAt = entity.ControllerViewedAt,
            ConditionsFulfilled = entity.ConditionsFulfilled,
            ExcludedFromCompensation = entity.ExcludedFromCompensation
        };

    public static EvaluationDetailResponse ToDetail(EvaluationEntity entity)
    {
        var summary = ToSummary(entity);
        return new EvaluationDetailResponse
        {
            Id = summary.Id,
            EmployeeId = summary.EmployeeId,
            EmployeeFullName = summary.EmployeeFullName,
            OrganizationUnitName = summary.OrganizationUnitName,
            EvaluatorEmployeeId = summary.EvaluatorEmployeeId,
            EvaluatorFullName = summary.EvaluatorFullName,
            ControllerEmployeeId = summary.ControllerEmployeeId,
            ControllerFullName = summary.ControllerFullName,
            Year = summary.Year,
            Quarter = summary.Quarter,
            Status = summary.Status,
            OverallAverage = summary.OverallAverage,
            DescriptiveRatingName = summary.DescriptiveRatingName,
            SubmittedAt = summary.SubmittedAt,
            ApprovedAt = summary.ApprovedAt,
            Version = summary.Version,
            GoalCount = summary.GoalCount,
            GoalsPlanningComplete = summary.GoalsPlanningComplete,
            HasIncompleteRatings = summary.HasIncompleteRatings,
            ControllerViewedAt = summary.ControllerViewedAt,
            ConditionsFulfilled = summary.ConditionsFulfilled,
            ExcludedFromCompensation = summary.ExcludedFromCompensation,
            GoalsAverage = entity.GoalsAverage,
            MeasuresAverage = entity.MeasuresAverage,
            DescriptiveRatingId = entity.DescriptiveRatingId,
            ConversationAt = entity.ConversationAt,
            ReviewedAt = entity.ReviewedAt,
            EvaluatorComment = entity.EvaluatorComment,
            ConditionsNotMetComment = entity.ConditionsNotMetComment,
            ControllerComment = entity.ControllerComment,
            RejectionReason = entity.RejectionReason,
            Goals = entity.Goals.OrderBy(g => g.SortOrder).Select(ToGoal).ToList(),
            Measures = entity.Measures.OrderBy(m => m.SortOrder).Select(ToMeasure).ToList(),
            Criteria = entity.Criteria.OrderBy(c => c.SortOrder).Select(ToCriterion).ToList(),
            Conditions = entity.Conditions.OrderBy(c => c.SortOrder).Select(ToCondition).ToList(),
            Training = entity.Training is null ? null : ToTraining(entity.Training)
        };
    }

    public static EvaluationGoalResponse ToGoal(EvaluationGoal entity) =>
        new()
        {
            Id = entity.Id,
            Description = entity.Description,
            RatingLevelId = entity.RatingLevelId,
            RatingLevelValue = entity.RatingLevel?.Value ?? 0,
            RatingLevelLabel = entity.RatingLevel?.Label ?? string.Empty,
            Comment = entity.Comment,
            Weight = entity.Weight,
            SortOrder = entity.SortOrder
        };

    public static EvaluationMeasureResponse ToMeasure(EvaluationMeasure entity) =>
        new()
        {
            Id = entity.Id,
            MeasureTypeId = entity.MeasureTypeId,
            MeasureTypeName = entity.MeasureType?.Name ?? string.Empty,
            MeasureDescriptionId = entity.MeasureDescriptionId,
            MeasureDescription = entity.MeasureDescription?.Description ?? entity.CustomDescription,
            CustomDescription = entity.CustomDescription,
            RatingComment = entity.RatingComment,
            RatingLevelId = entity.RatingLevelId,
            RatingLevelValue = entity.RatingLevel?.Value ?? 0,
            RatingLevelLabel = entity.RatingLevel?.Label ?? string.Empty,
            SortOrder = entity.SortOrder
        };

    public static EvaluationCriterionResponse ToCriterion(EvaluationCriterion entity) =>
        new() { Id = entity.Id, Description = entity.Description, SortOrder = entity.SortOrder };

    public static EvaluationConditionResponse ToCondition(EvaluationCondition entity) =>
        new() { Id = entity.Id, Description = entity.Description, SortOrder = entity.SortOrder };

    public static EvaluationTrainingResponse ToTraining(EvaluationTraining entity) =>
        new()
        {
            Id = entity.Id,
            TrainingDescription = entity.TrainingDescription,
            KnowledgeDescription = entity.KnowledgeDescription,
            DevelopmentDescription = entity.DevelopmentDescription,
            EvaluatorComment = entity.EvaluatorComment
        };

    public static EvaluationStatusHistoryResponse ToStatusHistory(EvaluationStatusHistory entity) =>
        new()
        {
            Id = entity.Id,
            FromStatus = entity.FromStatus,
            ToStatus = entity.ToStatus,
            ChangedByUserId = entity.ChangedByUserId,
            Comment = entity.Comment,
            ChangedAt = entity.ChangedAt
        };

    public static RatingLevelResponse ToRatingLevel(RatingLevel entity) =>
        new()
        {
            Id = entity.Id,
            Value = entity.Value,
            Label = entity.Label,
            Description = entity.Description
        };

    public static DescriptiveRatingResponse ToDescriptiveRating(DescriptiveRating entity) =>
        new()
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            MinAverage = entity.MinAverage,
            MaxAverage = entity.MaxAverage,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            RecommendedShare = entity.RecommendedShare,
        };

    public static MeasureTypeResponse ToMeasureType(MeasureType entity, bool includeDescriptions) =>
        new()
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            SortOrder = entity.SortOrder,
            Descriptions = includeDescriptions
                ? entity.Descriptions.OrderBy(d => d.Id).Select(d => new MeasureTypeDescriptionResponse
                {
                    Id = d.Id,
                    Description = d.Description
                }).ToList()
                : Array.Empty<MeasureTypeDescriptionResponse>()
        };
}
