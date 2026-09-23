using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Auth;

/// <summary>
/// A session works in one role at a time, and the access token carries only
/// that role, so every role check sees the role the user is working in.
/// </summary>
internal static class ActiveRoles
{
    /// <summary>
    /// The role a session runs in: the requested one while the user still holds
    /// it, then the role they last worked in, then the first of their roles by
    /// <see cref="RoleCodes.SessionPriority"/>. Null only for a user without
    /// roles.
    /// </summary>
    internal static string? ForSession(
        IReadOnlyList<string> userRoles,
        string? requestedRole,
        string? lastActiveRole = null)
    {
        return Find(userRoles, requestedRole)
            ?? Find(userRoles, lastActiveRole)
            ?? RoleCodes.SessionPriority.FirstOrDefault(userRoles.Contains);
    }

    /// <summary>The role claims of a session: the one role it works in, or none for a user without roles.</summary>
    internal static IEnumerable<string> AsClaims(string? activeRole) =>
        activeRole is null ? [] : [activeRole];

    internal static string? Find(IReadOnlyList<string> userRoles, string? roleCode) =>
        roleCode is null
            ? null
            : userRoles.FirstOrDefault(role => string.Equals(role, roleCode.Trim(), StringComparison.OrdinalIgnoreCase));
}
