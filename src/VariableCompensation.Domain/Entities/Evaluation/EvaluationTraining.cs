namespace VariableCompensation.Domain.Entities.Evaluation;

public class EvaluationTraining
{
    public long Id { get; set; }

    public long EvaluationId { get; set; }

    public Evaluation Evaluation { get; set; } = null!;

    public string? TrainingDescription { get; set; }

    public string? KnowledgeDescription { get; set; }

    public string? DevelopmentDescription { get; set; }

    public string? EvaluatorComment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
