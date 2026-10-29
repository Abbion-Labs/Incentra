namespace VariableCompensation.Application.Hr.Models;

public sealed class EvaluatorSettingsResponse
{
    public long EmployeeId { get; init; }

    /// <summary>Sent back with an edit; see <c>IVersioned</c>.</summary>
    public int Version { get; init; }

    public string EmployeeFullName { get; init; } = string.Empty;

    /// <summary>Null when the evaluator has no controller and their evaluations need no review.</summary>
    public long? ControllerEmployeeId { get; init; }

    public string? ControllerFullName { get; init; }
}
