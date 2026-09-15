using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUserRepository userRepository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly IJwtTokenService jwtTokenService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IJwtTokenService jwtTokenService)
    {
        this.userRepository = userRepository;
        this.employeeRepository = employeeRepository;
        this.jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Failure<AuthResponse>(ErrorCodes.RefreshTokenRequired);
        }

        var tokenHash = LoginCommandHandler.HashToken(request.RefreshToken);
        var storedToken = await this.userRepository.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);
        if (storedToken is null || storedToken.RevokedAt is not null || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.RefreshTokenInvalid);
        }

        var user = await this.userRepository.FindByIdWithRolesAsync(storedToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.UserInactive);
        }

        storedToken.RevokedAt = DateTime.UtcNow;

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var accessToken = this.jwtTokenService.GenerateAccessToken(user, roles);
        var refreshTokenPlain = this.jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = new Domain.Entities.Identity.RefreshToken
        {
            UserId = user.Id,
            TokenHash = LoginCommandHandler.HashToken(refreshTokenPlain),
            ExpiresAt = this.jwtTokenService.GetRefreshTokenExpiry()
        };

        await this.userRepository.AddRefreshTokenAsync(newRefreshToken, cancellationToken);
        await this.userRepository.SaveChangesAsync(cancellationToken);

        var employee = await this.employeeRepository.FindByUserIdAsync(user.Id, cancellationToken);

        return Result.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenPlain,
            AccessTokenExpiresAt = this.jwtTokenService.GetAccessTokenExpiry(),
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
            User = LoginCommandHandler.MapProfile(user, roles, employee)
        });
    }
}
