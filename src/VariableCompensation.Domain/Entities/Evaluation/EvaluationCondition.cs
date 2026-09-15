namespace VariableCompensation.Domain.Entities.Evaluation;

public class EvaluationCondition
{
    public long Id { get; set; }

    public long EvaluationId { get; set; }

    public Evaluation Evaluation { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
