namespace VariableCompensation.Domain.Entities.Hr;

public class EvaluatorSettings
{
    public long EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Who reviews this evaluator's evaluations. None for an evaluator at the top of the organization: nobody
    /// reviews them, so their evaluations are approved as soon as they are submitted.
    /// </summary>
    public long? ControllerEmployeeId { get; set; }

    public Employee? Controller { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
