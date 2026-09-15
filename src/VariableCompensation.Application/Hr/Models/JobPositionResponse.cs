namespace VariableCompensation.Application.Hr.Models;

public sealed class JobPositionResponse
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }
}
