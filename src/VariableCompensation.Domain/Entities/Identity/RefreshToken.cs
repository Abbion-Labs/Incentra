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

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Set when the token is exchanged for a new one, or when its session is signed out.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
