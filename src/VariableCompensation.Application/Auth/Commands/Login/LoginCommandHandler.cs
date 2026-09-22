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
    private readonly ILoginAttemptLimiter loginAttemptLimiter;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILoginAttemptLimiter loginAttemptLimiter)
    {
        this.userRepository = userRepository;
        this.employeeRepository = employeeRepository;
        this.passwordHasher = passwordHasher;
        this.jwtTokenService = jwtTokenService;
        this.loginAttemptLimiter = loginAttemptLimiter;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var retryAfter = this.loginAttemptLimiter.GetRetryAfter(email);
        if (retryAfter is not null)
        {
            return Result.Failure<AuthResponse>(
                $"{ErrorCodes.TooManyLoginAttempts}?seconds={(int)Math.Ceiling(retryAfter.Value.TotalSeconds)}");
        }

        var user = await this.userRepository.FindByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive || !this.passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            this.loginAttemptLimiter.RecordFailure(email);
            return Result.Failure<AuthResponse>(ErrorCodes.InvalidEmailOrPassword);
        }

        this.loginAttemptLimiter.Reset(email);
        user.LastLoginAt = DateTime.UtcNow;
        await this.userRepository.DeleteExpiredRefreshTokensAsync(user.Id, cancellationToken);
        var authResponse = await this.IssueTokensAsync(user, cancellationToken);
        await this.userRepository.SaveChangesAsync(cancellationToken);
        return authResponse;
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();
        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var accessToken = this.jwtTokenService.GenerateAccessToken(user, roles, sessionId);
        var refreshTokenPlain = this.jwtTokenService.GenerateRefreshToken();
        var refreshToken = new Domain.Entities.Identity.RefreshToken
        {
            UserId = user.Id,
            SessionId = sessionId,
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
