namespace VariableCompensation.Api.Contracts.Evaluation;

public sealed class CreateEvaluationRequest
{
    public long EmployeeId { get; init; }

    public short Year { get; init; }

    public byte Quarter { get; init; }
}

public sealed class UpdateEvaluationDraftRequest
{
    public int Version { get; init; }

    public DateTime? ConversationAt { get; init; }

    public string? EvaluatorComment { get; init; }

    public string? ConditionsNotMetComment { get; init; }

    public bool ConditionsFulfilled { get; init; } = true;
}

public sealed class EvaluationGoalItemRequest
{
    public string Description { get; init; } = string.Empty;

    public long? RatingLevelId { get; init; }

    public string? Comment { get; init; }

    public decimal? Weight { get; init; }

    public int SortOrder { get; init; }
}

public sealed class ReplaceEvaluationGoalsRequest
{
    public int Version { get; init; }

    public IReadOnlyList<EvaluationGoalItemRequest> Goals { get; init; } = Array.Empty<EvaluationGoalItemRequest>();
}

public sealed class EvaluationMeasureItemRequest
{
    public long MeasureTypeId { get; init; }

    public string? RatingComment { get; init; }

    public long RatingLevelId { get; init; }

    public int SortOrder { get; init; }
}

public sealed class ReplaceEvaluationMeasuresRequest
{
    public int Version { get; init; }

    public IReadOnlyList<EvaluationMeasureItemRequest> Measures { get; init; } = Array.Empty<EvaluationMeasureItemRequest>();
}

public sealed class EvaluationCriterionItemRequest
{
    public string Description { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed class ReplaceEvaluationCriteriaRequest
{
    public int Version { get; init; }

    public IReadOnlyList<EvaluationCriterionItemRequest> Criteria { get; init; } = Array.Empty<EvaluationCriterionItemRequest>();
}

public sealed class EvaluationConditionItemRequest
{
    public string Description { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed class ReplaceEvaluationConditionsRequest
{
    public int Version { get; init; }

    public IReadOnlyList<EvaluationConditionItemRequest> Conditions { get; init; } = Array.Empty<EvaluationConditionItemRequest>();
}

public sealed class UpsertEvaluationTrainingRequest
{
    public int Version { get; init; }

    public string? TrainingDescription { get; init; }

    public string? KnowledgeDescription { get; init; }

    public string? DevelopmentDescription { get; init; }

    public string? EvaluatorComment { get; init; }
}

public sealed class EvaluationVersionRequest
{
    public int Version { get; init; }
}

public sealed class ApproveEvaluationRequest
{
    public int Version { get; init; }

    public string? ControllerComment { get; init; }
}

public sealed class ReturnEvaluationForRevisionRequest
{
    public int Version { get; init; }

    public string RevisionComment { get; init; } = string.Empty;
}

public sealed class SaveEvaluationPlanningDraftRequest
{
    public int Version { get; init; }

    public DateTime? ConversationAt { get; init; }

    public string? EvaluatorComment { get; init; }

    public string? ConditionsNotMetComment { get; init; }

    public bool ConditionsFulfilled { get; init; } = true;

    public IReadOnlyList<EvaluationGoalItemRequest> Goals { get; init; } = Array.Empty<EvaluationGoalItemRequest>();

    public IReadOnlyList<EvaluationConditionItemRequest> Conditions { get; init; } = Array.Empty<EvaluationConditionItemRequest>();

    public IReadOnlyList<EvaluationCriterionItemRequest> Criteria { get; init; } = Array.Empty<EvaluationCriterionItemRequest>();
}

public sealed class SaveEvaluationRatingDraftRequest
{
    public int Version { get; init; }

    public DateTime? ConversationAt { get; init; }

    public string? EvaluatorComment { get; init; }

    public string? ConditionsNotMetComment { get; init; }

    public bool ConditionsFulfilled { get; init; } = true;

    public IReadOnlyList<EvaluationGoalItemRequest>? Goals { get; init; }

    public IReadOnlyList<EvaluationMeasureItemRequest>? Measures { get; init; }

    public UpsertEvaluationTrainingRequest? Training { get; init; }
}
