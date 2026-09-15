namespace VariableCompensation.Application.Auth.Models;

public sealed class UserProfileResponse
{
    public long Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public long? EmployeeId { get; init; }

    public string? EmployeeFullName { get; init; }

    public string? EmployeeFirstName { get; init; }

    public string? EmployeeLastName { get; init; }

    public string? EmployeeAvatarUrl { get; init; }

    public bool EmailNotificationsEnabled { get; init; }
}
