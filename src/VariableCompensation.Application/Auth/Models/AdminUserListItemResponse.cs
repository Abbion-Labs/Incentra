namespace VariableCompensation.Application.Auth.Models;

public sealed class AdminUserListItemResponse
{
    public long Id { get; init; }

    /// <summary>Sent back with an edit; see <c>IVersioned</c>.</summary>
    public int Version { get; init; }

    public string Email { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool IsActive { get; init; }

    public long? EmployeeId { get; init; }

    public string? EmployeeFullName { get; init; }
}
