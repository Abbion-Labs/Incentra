namespace VariableCompensation.Application.Evaluation.Models;

public class EvaluationSummaryResponse
{
    public long Id { get; init; }

    public long EmployeeId { get; init; }

    public string EmployeeFullName { get; init; } = string.Empty;

    public string OrganizationUnitName { get; init; } = string.Empty;

    public long EvaluatorEmployeeId { get; init; }

    public string EvaluatorFullName { get; init; } = string.Empty;

    public long? ControllerEmployeeId { get; init; }

    public string? ControllerFullName { get; init; }

    public short Year { get; init; }

    public byte Quarter { get; init; }

    public string Status { get; init; } = string.Empty;

    public decimal? OverallAverage { get; init; }

    public string? DescriptiveRatingName { get; init; }

    public DateTime? SubmittedAt { get; init; }

    public DateTime? ApprovedAt { get; init; }

    public int Version { get; init; }

    public int GoalCount { get; init; }

    public bool GoalsPlanningComplete { get; init; }

    public bool HasIncompleteRatings { get; init; }

    public string? ControllerComment { get; init; }

    public DateTime? ControllerViewedAt { get; init; }

    public bool ConditionsFulfilled { get; init; } = true;

    public bool ExcludedFromCompensation { get; init; }
}

public sealed class EvaluationDetailResponse : EvaluationSummaryResponse
{
    public decimal? GoalsAverage { get; init; }

    public decimal? MeasuresAverage { get; init; }

    public long? DescriptiveRatingId { get; init; }

    public DateTime? ConversationAt { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public string? EvaluatorComment { get; init; }

    public string? ConditionsNotMetComment { get; init; }

    public string? ControllerComment { get; init; }

    public string? RejectionReason { get; init; }

    public IReadOnlyList<EvaluationGoalResponse> Goals { get; init; } = Array.Empty<EvaluationGoalResponse>();

    public IReadOnlyList<EvaluationMeasureResponse> Measures { get; init; } = Array.Empty<EvaluationMeasureResponse>();

    public IReadOnlyList<EvaluationCriterionResponse> Criteria { get; init; } = Array.Empty<EvaluationCriterionResponse>();

    public IReadOnlyList<EvaluationConditionResponse> Conditions { get; init; } = Array.Empty<EvaluationConditionResponse>();

    public EvaluationTrainingResponse? Training { get; init; }
}

public sealed class EvaluationGoalResponse
{
    public long Id { get; init; }

    public string Description { get; init; } = string.Empty;

    public long RatingLevelId { get; init; }

    public int RatingLevelValue { get; init; }

    public string RatingLevelLabel { get; init; } = string.Empty;

    public string? Comment { get; init; }

    public decimal? Weight { get; init; }

    public int SortOrder { get; init; }
}

public sealed class EvaluationMeasureResponse
{
    public long Id { get; init; }

    public long MeasureTypeId { get; init; }

    public string MeasureTypeName { get; init; } = string.Empty;

    public string? RatingComment { get; init; }

    public long RatingLevelId { get; init; }

    public int RatingLevelValue { get; init; }

    public string RatingLevelLabel { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed class EvaluationCriterionResponse
{
    public long Id { get; init; }

    public string Description { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed class EvaluationConditionResponse
{
    public long Id { get; init; }

    public string Description { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed class EvaluationTrainingResponse
{
    public long Id { get; init; }

    public string? TrainingDescription { get; init; }

    public string? KnowledgeDescription { get; init; }

    public string? DevelopmentDescription { get; init; }

    public string? EvaluatorComment { get; init; }
}

public sealed class EvaluationStatusHistoryResponse
{
    public long Id { get; init; }

    public string? FromStatus { get; init; }

    public string ToStatus { get; init; } = string.Empty;

    public long ChangedByUserId { get; init; }

    public string? Comment { get; init; }

    public DateTime ChangedAt { get; init; }
}

public sealed class RatingLevelResponse
{
    public long Id { get; init; }

    public int Value { get; init; }

    public string Label { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public sealed class DescriptiveRatingResponse
{
    public long Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal? MinAverage { get; init; }

    public decimal? MaxAverage { get; init; }

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }

    public decimal RecommendedShare { get; init; }
}

public sealed class MeasureTypeResponse
{
    public long Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int SortOrder { get; init; }
}
