namespace VariableCompensation.Application.Auth.Models;

public sealed class AdminUserListItemResponse
{
    public long Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool IsActive { get; init; }

    public long? EmployeeId { get; init; }

    public string? EmployeeFullName { get; init; }
}
