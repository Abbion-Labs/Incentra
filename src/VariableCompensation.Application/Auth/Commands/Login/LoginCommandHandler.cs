using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUserRepository userRepository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IJwtTokenService jwtTokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        this.userRepository = userRepository;
        this.employeeRepository = employeeRepository;
        this.passwordHasher = passwordHasher;
        this.jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await this.userRepository.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.InvalidEmailOrPassword);
        }

        if (!this.passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResponse>(ErrorCodes.InvalidEmailOrPassword);
        }

        user.LastLoginAt = DateTime.UtcNow;
        var authResponse = await this.IssueTokensAsync(user, cancellationToken);
        await this.userRepository.SaveChangesAsync(cancellationToken);
        return authResponse;
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var accessToken = this.jwtTokenService.GenerateAccessToken(user, roles);
        var refreshTokenPlain = this.jwtTokenService.GenerateRefreshToken();
        var refreshToken = new Domain.Entities.Identity.RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshTokenPlain),
            ExpiresAt = this.jwtTokenService.GetRefreshTokenExpiry()
        };

        await this.userRepository.AddRefreshTokenAsync(refreshToken, cancellationToken);

        var employee = await this.employeeRepository.FindByUserIdAsync(user.Id, cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenPlain,
            AccessTokenExpiresAt = this.jwtTokenService.GetAccessTokenExpiry(),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = MapProfile(user, roles, employee)
        };
    }

    internal static UserProfileResponse MapProfile(User user, IReadOnlyList<string> roles, Domain.Entities.Hr.Employee? employee = null) =>
        UserProfileMapper.Map(user, roles, employee);

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
