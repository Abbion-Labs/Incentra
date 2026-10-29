using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Application.Auth;

/// <summary>
/// Builds the token pair a session runs on. The access token carries the session's id and only the role the
/// session works in, and the refresh token remembers that role so rotations keep it. Remembering the role on
/// the account is left to the command that represents the user's choice. The caller saves changes.
/// </summary>
public sealed class AuthSessionIssuer
{
    private readonly IUserRepository userRepository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly IJwtTokenService jwtTokenService;

    public AuthSessionIssuer(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IJwtTokenService jwtTokenService)
    {
        this.userRepository = userRepository;
        this.employeeRepository = employeeRepository;
        this.jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Starts a session in <paramref name="requestedRole"/> when the user holds it, otherwise in the role they
    /// last worked in or their first one.
    /// </summary>
    public async Task<AuthResponse> StartSessionAsync(
        User user,
        string? requestedRole,
        CancellationToken cancellationToken)
    {
        var refreshTokenPlain = this.jwtTokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            SessionId = Guid.NewGuid(),
            TokenHash = LoginCommandHandler.HashToken(refreshTokenPlain),
            ExpiresAt = this.jwtTokenService.GetRefreshTokenExpiry(),
            ActiveRoleCode = ActiveRoles.ForSession(RolesOf(user), requestedRole, user.LastActiveRoleCode),
        };

        await this.userRepository.AddRefreshTokenAsync(refreshToken, cancellationToken);

        return await this.DescribeAsync(user, refreshToken, refreshTokenPlain, cancellationToken);
    }

    /// <summary>
    /// The response for a token pair that is already stored, used by both a new session and a rotation.
    /// </summary>
    public async Task<AuthResponse> DescribeAsync(
        User user,
        RefreshToken refreshToken,
        string refreshTokenPlain,
        CancellationToken cancellationToken)
    {
        var roles = RolesOf(user);
        var employee = await this.employeeRepository.FindByUserIdAsync(user.Id, cancellationToken);

        return new AuthResponse
        {
            AccessToken = this.jwtTokenService.GenerateAccessToken(
                user,
                ActiveRoles.AsClaims(refreshToken.ActiveRoleCode),
                refreshToken.SessionId),
            RefreshToken = refreshTokenPlain,
            AccessTokenExpiresAt = this.jwtTokenService.GetAccessTokenExpiry(),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = UserProfileMapper.Map(user, roles, employee, refreshToken.ActiveRoleCode),
        };
    }

    private static IReadOnlyList<string> RolesOf(User user) =>
        user.UserRoles.Select(ur => ur.Role.Code).ToList();
}
