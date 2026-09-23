namespace VariableCompensation.Domain.Entities.Hr;

public class EvaluatorSettings
{
    public long EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public long ControllerEmployeeId { get; set; }

    public Employee Controller { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
