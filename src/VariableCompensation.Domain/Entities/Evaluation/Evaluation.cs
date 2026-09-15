using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Domain.Entities.Evaluation;

public class Evaluation
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public Hr.Employee Employee { get; set; } = null!;

    public long EvaluatorEmployeeId { get; set; }

    public Hr.Employee Evaluator { get; set; } = null!;

    public long? ControllerEmployeeId { get; set; }

    public Hr.Employee? Controller { get; set; }

    public short Year { get; set; }

    public byte Quarter { get; set; }

    public EvaluationStatus Status { get; set; } = EvaluationStatus.Draft;

    public decimal? GoalsAverage { get; set; }

    public decimal? MeasuresAverage { get; set; }

    public decimal? OverallAverage { get; set; }

    public long? DescriptiveRatingId { get; set; }

    public Lookup.DescriptiveRating? DescriptiveRating { get; set; }

    public DateTime? ConversationAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? ControllerViewedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? EvaluatorComment { get; set; }

    public string? ConditionsNotMetComment { get; set; }

    public string? ControllerComment { get; set; }

    public string? RejectionReason { get; set; }

    public bool ConditionsFulfilled { get; set; } = true;

    public bool ExcludedFromCompensation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public long? CreatedByUserId { get; set; }

    public long? UpdatedByUserId { get; set; }

    public int Version { get; set; } = 1;

    public ICollection<EvaluationGoal> Goals { get; set; } = new List<EvaluationGoal>();

    public ICollection<EvaluationCriterion> Criteria { get; set; } = new List<EvaluationCriterion>();

    public ICollection<EvaluationMeasure> Measures { get; set; } = new List<EvaluationMeasure>();

    public EvaluationTraining? Training { get; set; }

    public ICollection<EvaluationCondition> Conditions { get; set; } = new List<EvaluationCondition>();

    public ICollection<EvaluationStatusHistory> StatusHistory { get; set; } = new List<EvaluationStatusHistory>();
}
