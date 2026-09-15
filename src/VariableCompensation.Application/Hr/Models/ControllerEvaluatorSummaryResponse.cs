namespace VariableCompensation.Application.Hr.Models;

public sealed class ControllerEvaluatorSummaryResponse
{
    public long EmployeeId { get; init; }

    public string EmployeeFullName { get; init; } = string.Empty;

    public string OrganizationUnitName { get; init; } = string.Empty;

    public string JobPositionName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public int SubordinateCount { get; init; }
}
