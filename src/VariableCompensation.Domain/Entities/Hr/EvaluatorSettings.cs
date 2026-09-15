namespace VariableCompensation.Domain.Entities.Hr;

public class EvaluatorSettings
{
    public long EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public long ControllerEmployeeId { get; set; }

    public Employee Controller { get; set; } = null!;

    public decimal ThresholdDoesNotMeet { get; set; }

    public decimal ThresholdMeets { get; set; }

    public decimal ThresholdGood { get; set; }

    public decimal ThresholdExceeds { get; set; }

    public decimal PercentDoesNotMeet { get; set; }

    public decimal PercentMeets { get; set; }

    public decimal PercentGood { get; set; }

    public decimal PercentExceeds { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
