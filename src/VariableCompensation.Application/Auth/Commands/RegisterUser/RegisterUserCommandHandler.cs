using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Application.Auth.Commands.RegisterUser;

public interface IRoleLookup
{
    Task<long?> FindRoleIdByCodeAsync(string roleCode, CancellationToken cancellationToken);
}

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponse>>
{
    private readonly IUserRepository userRepository;
    private readonly IRoleLookup roleLookup;
    private readonly IPasswordHasher passwordHasher;
    private readonly IJwtTokenService jwtTokenService;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRoleLookup roleLookup,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        this.userRepository = userRepository;
        this.roleLookup = roleLookup;
        this.passwordHasher = passwordHasher;
        this.jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Result.Failure<AuthResponse>(ErrorCodes.EmailInvalid);
        }

        if (request.Password.Length < 8)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.PasswordTooShort);
        }

        if (await this.userRepository.EmailExistsAsync(email, null, cancellationToken))
        {
            return Result.Failure<AuthResponse>(ErrorCodes.EmailAlreadyRegistered);
        }

        var roleIdsResult = await UserRoleSync.ResolveRoleIdsAsync(request.RoleCodes, this.roleLookup, cancellationToken);
        if (roleIdsResult.IsFailure)
        {
            return Result.Failure<AuthResponse>(roleIdsResult.Error);
        }

        var user = new User
        {
            Email = email,
            PasswordHash = this.passwordHasher.Hash(request.Password),
            IsActive = true,
            EmailVerifiedAt = DateTime.UtcNow
        };

        UserRoleSync.ApplyRoles(user, roleIdsResult.Value);

        await this.userRepository.AddUserAsync(user, cancellationToken);
        await this.userRepository.SaveChangesAsync(cancellationToken);

        var reloaded = await this.userRepository.FindByIdWithRolesAsync(user.Id, cancellationToken);
        if (reloaded is null)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.CreateUserReloadFailed);
        }

        var roles = reloaded.UserRoles.Select(ur => ur.Role.Code).ToList();
        var accessToken = this.jwtTokenService.GenerateAccessToken(reloaded, roles);
        var refreshTokenPlain = this.jwtTokenService.GenerateRefreshToken();
        var refreshToken = new Domain.Entities.Identity.RefreshToken
        {
            UserId = reloaded.Id,
            TokenHash = LoginCommandHandler.HashToken(refreshTokenPlain),
            ExpiresAt = this.jwtTokenService.GetRefreshTokenExpiry()
        };

        await this.userRepository.AddRefreshTokenAsync(refreshToken, cancellationToken);
        await this.userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenPlain,
            AccessTokenExpiresAt = this.jwtTokenService.GetAccessTokenExpiry(),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = LoginCommandHandler.MapProfile(reloaded, roles)
        });
    }
}
