namespace VariableCompensation.Application.Auth.Models;

public sealed class UserProfileResponse
{
    public long Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The role the session works in, one of <see cref="Roles"/>. Null only for
    /// a user without roles.
    /// </summary>
    public string? ActiveRole { get; init; }

    public long? EmployeeId { get; init; }

    public string? EmployeeFullName { get; init; }

    public string? EmployeeFirstName { get; init; }

    public string? EmployeeLastName { get; init; }

    public string? EmployeeAvatarUrl { get; init; }

    public bool EmailNotificationsEnabled { get; init; }
}
