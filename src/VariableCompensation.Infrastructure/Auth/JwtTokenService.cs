using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private const int RefreshTokenBytes = 32;

    private readonly JwtSettings settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        this.settings = settings.Value;
    }

    public TimeSpan RefreshTokenReuseGracePeriod => TimeSpan.FromSeconds(this.settings.RefreshTokenReuseGraceSeconds);

    public string GenerateAccessToken(User user, IEnumerable<string> roles, Guid sessionId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sid, sessionId.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = this.GetAccessTokenExpiry();

        var token = new JwtSecurityToken(
            issuer: this.settings.Issuer,
            audience: this.settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

    public DateTime GetAccessTokenExpiry() => DateTime.UtcNow.AddMinutes(this.settings.AccessTokenMinutes);

    public DateTime GetRefreshTokenExpiry() => DateTime.UtcNow.AddDays(this.settings.RefreshTokenDays);
}
