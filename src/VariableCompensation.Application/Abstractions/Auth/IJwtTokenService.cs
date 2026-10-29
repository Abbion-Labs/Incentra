using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Application.Abstractions.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, Guid sessionId);

    string GenerateRefreshToken();

    DateTime GetAccessTokenExpiry();

    DateTime GetRefreshTokenExpiry();

    /// <summary>
    /// How long a refresh token that has just been exchanged may still be exchanged again. Presenting it later
    /// than that is treated as a replay and ends the session.
    /// </summary>
    TimeSpan RefreshTokenReuseGracePeriod { get; }
}
