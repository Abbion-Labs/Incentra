using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain;

namespace VariableCompensation.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUserRepository userRepository;
    private readonly AuthSessionIssuer sessionIssuer;
    private readonly IJwtTokenService jwtTokenService;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<RefreshTokenCommandHandler> logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        AuthSessionIssuer sessionIssuer,
        IJwtTokenService jwtTokenService,
        TimeProvider timeProvider,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        this.userRepository = userRepository;
        this.sessionIssuer = sessionIssuer;
        this.jwtTokenService = jwtTokenService;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Failure<AuthResponse>(ErrorCodes.RefreshTokenRequired);
        }

        var now = this.timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = LoginCommandHandler.HashToken(request.RefreshToken);
        var storedToken = await this.userRepository.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);
        if (storedToken is null || storedToken.ExpiresAt <= now)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.RefreshTokenInvalid);
        }

        var user = await this.userRepository.FindByIdWithRolesAsync(storedToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.UserInactive);
        }

        await this.userRepository.DeleteExpiredRefreshTokensAsync(user.Id, cancellationToken);

        var refreshTokenPlain = this.jwtTokenService.GenerateRefreshToken();
        var successor = new Domain.Entities.Identity.RefreshToken
        {
            UserId = user.Id,
            SessionId = storedToken.SessionId,
            TokenHash = LoginCommandHandler.HashToken(refreshTokenPlain),
            ExpiresAt = this.jwtTokenService.GetRefreshTokenExpiry(),

            // The session carries on in its own role. A session from before the role was carried has none,
            // and falls back the same way a new session would.
            ActiveRoleCode = ActiveRoles.ForSession(
                user.UserRoles.Select(ur => ur.Role.Code).ToList(),
                storedToken.ActiveRoleCode,
                user.LastActiveRoleCode),
        };

        if (storedToken.RevokedAt is null
            && await this.userRepository.TryRotateRefreshTokenAsync(storedToken, successor, now, cancellationToken))
        {
            return await this.sessionIssuer.DescribeAsync(user, successor, refreshTokenPlain, cancellationToken);
        }

        // The token has been exchanged before, or a concurrent refresh with it got there first a moment ago.
        var exchangedAt = storedToken.RevokedAt ?? now;

        if (!await this.userRepository.IsSessionActiveAsync(user.Id, storedToken.SessionId, now, cancellationToken))
        {
            // Signed out, the password was changed, or a replay was already caught: the session is over.
            return Result.Failure<AuthResponse>(ErrorCodes.RefreshTokenInvalid);
        }

        if (now - exchangedAt <= this.jwtTokenService.RefreshTokenReuseGracePeriod)
        {
            // Almost certainly the same browser: the page was reloaded before the previous response arrived, or
            // two tabs refreshed at once. It carries on in the same session.
            await this.userRepository.AddRefreshTokenAsync(successor, cancellationToken);
            await this.userRepository.SaveChangesAsync(cancellationToken);
            return await this.sessionIssuer.DescribeAsync(user, successor, refreshTokenPlain, cancellationToken);
        }

        // An exchanged token came back long after its successor took over, so a copy of it is in someone else's
        // hands. There is no telling which side is the copy, so the session ends for both.
        await this.userRepository.RevokeSessionAsync(storedToken.SessionId, cancellationToken);
        await this.userRepository.SaveChangesAsync(cancellationToken);
        this.logger.LogWarning(
            "A refresh token of user {UserId} was presented again after it had been exchanged; session {SessionId} was signed out.",
            user.Id,
            storedToken.SessionId);

        return Result.Failure<AuthResponse>(ErrorCodes.RefreshTokenInvalid);
    }
}
