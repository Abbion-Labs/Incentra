namespace VariableCompensation.Application.Hr.Models;

public sealed class EvaluatorSettingsResponse
{
    public long EmployeeId { get; init; }

    public string EmployeeFullName { get; init; } = string.Empty;

    public long ControllerEmployeeId { get; init; }

    public string ControllerFullName { get; init; } = string.Empty;
}
