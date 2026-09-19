namespace VariableCompensation.Infrastructure.Auth;

public sealed class LoginRateLimitOptions
{
    public const string SectionName = "LoginRateLimit";

    public int MaxFailedAttempts { get; set; } = 5;

    public int WindowSeconds { get; set; } = 60;
}
