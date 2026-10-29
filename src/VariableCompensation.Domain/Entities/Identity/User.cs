using VariableCompensation.Domain.Common;

namespace VariableCompensation.Domain.Entities.Identity;

public class User : IVersioned
{
    public long Id { get; set; }

    public int Version { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool EmailNotificationsEnabled { get; private set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// The role this account last worked in. Signing in starts in it again, on
    /// any device, as long as the user still holds it.
    /// </summary>
    public string? LastActiveRoleCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public Hr.Employee? Employee { get; set; }

    public void SetEmailNotificationsEnabled(bool enabled)
    {
        this.EmailNotificationsEnabled = enabled;
        this.UpdatedAt = DateTime.UtcNow;
    }
}
