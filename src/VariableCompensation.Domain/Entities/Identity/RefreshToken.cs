namespace VariableCompensation.Domain.Entities.Identity;

public class RefreshToken
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// The sign-in this token belongs to. Login starts a session and every rotation passes it on, so all tokens
    /// issued from one login share it. Access tokens carry it as their <c>sid</c> claim, which is how signing out
    /// ends them too.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// The role the session works in. A refreshed access token keeps carrying only this role, so a user with
    /// several roles stays in the one they are working in until they switch or sign in again.
    /// </summary>
    public string? ActiveRoleCode { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Set when the token is exchanged for a new one, or when its session is signed out.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
