using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain;

namespace VariableCompensation.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly AuthSessionIssuer sessionIssuer;
    private readonly ILoginAttemptLimiter loginAttemptLimiter;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        AuthSessionIssuer sessionIssuer,
        ILoginAttemptLimiter loginAttemptLimiter)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.sessionIssuer = sessionIssuer;
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
        // The session starts in the role this account last worked in.
        var authResponse = await this.sessionIssuer.StartSessionAsync(user, requestedRole: null, cancellationToken);
        await this.userRepository.SaveChangesAsync(cancellationToken);
        return authResponse;
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
