namespace VariableCompensation.Domain.Entities.Compensation;

public class VariableCompensationEvaluationLink
{
    public long ResultId { get; set; }

    public VariableCompensationResult Result { get; set; } = null!;

    public long EvaluationId { get; set; }

    public Evaluation.Evaluation Evaluation { get; set; } = null!;
}
