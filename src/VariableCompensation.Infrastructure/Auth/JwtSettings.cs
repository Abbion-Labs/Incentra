namespace VariableCompensation.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>
    /// A refresh response can be lost, for example when the page is reloaded while it is on its way, and the
    /// browser then still holds the token that was just exchanged. Within this window that token is accepted
    /// once more instead of ending the session.
    /// </summary>
    public int RefreshTokenReuseGraceSeconds { get; set; } = 30;
}
