namespace VariableCompensation.Application.Hr.Models;

public sealed class OrganizationUnitResponse
{
    public long Id { get; init; }

    /// <summary>Sent back with an edit; see <c>IVersioned</c>.</summary>
    public int Version { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Code { get; init; }

    public bool IsActive { get; init; }
}
