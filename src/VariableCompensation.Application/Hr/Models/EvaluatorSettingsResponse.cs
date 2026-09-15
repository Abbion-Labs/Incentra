namespace VariableCompensation.Application.Hr.Models;

public sealed class EvaluatorSettingsResponse
{
    public long EmployeeId { get; init; }

    public string EmployeeFullName { get; init; } = string.Empty;

    public long ControllerEmployeeId { get; init; }

    public string ControllerFullName { get; init; } = string.Empty;

    public decimal ThresholdDoesNotMeet { get; init; }

    public decimal ThresholdMeets { get; init; }

    public decimal ThresholdGood { get; init; }

    public decimal ThresholdExceeds { get; init; }

    public decimal PercentDoesNotMeet { get; init; }

    public decimal PercentMeets { get; init; }

    public decimal PercentGood { get; init; }

    public decimal PercentExceeds { get; init; }
}
