using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Application.Abstractions.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);

    string GenerateRefreshToken();

    DateTime GetAccessTokenExpiry();

    DateTime GetRefreshTokenExpiry();
}
