namespace VariableCompensation.Application.Hr.Models;

public sealed class OrganizationUnitResponse
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Code { get; init; }

    public bool IsActive { get; init; }
}
