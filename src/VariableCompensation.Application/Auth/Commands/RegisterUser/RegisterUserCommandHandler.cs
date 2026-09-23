using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Auth.Commands.RegisterUser;

public interface IRoleLookup
{
    Task<long?> FindRoleIdByCodeAsync(string roleCode, CancellationToken cancellationToken);
}

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserProfileResponse>>
{
    private readonly IUserRepository userRepository;
    private readonly IRoleLookup roleLookup;
    private readonly IPasswordHasher passwordHasher;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRoleLookup roleLookup,
        IPasswordHasher passwordHasher)
    {
        this.userRepository = userRepository;
        this.roleLookup = roleLookup;
        this.passwordHasher = passwordHasher;
    }

    public async Task<Result<UserProfileResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Result.Failure<UserProfileResponse>(ErrorCodes.EmailInvalid);
        }

        if (request.Password.Length < 8)
        {
            return Result.Failure<UserProfileResponse>(ErrorCodes.PasswordTooShort);
        }

        if (await this.userRepository.EmailExistsAsync(email, null, cancellationToken))
        {
            return Result.Failure<UserProfileResponse>(ErrorCodes.EmailAlreadyRegistered);
        }

        // A new account has no employee linked yet -- that link is made from the
        // employee form -- so it cannot become an evaluator in the same step.
        if (request.RoleCodes.Contains(RoleCodes.Evaluator, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<UserProfileResponse>(ErrorCodes.EvaluatorUserNotLinkedToEmployee);
        }

        var roleIdsResult = await UserRoleSync.ResolveRoleIdsAsync(request.RoleCodes, this.roleLookup, cancellationToken);
        if (roleIdsResult.IsFailure)
        {
            return Result.Failure<UserProfileResponse>(roleIdsResult.Error);
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
            return Result.Failure<UserProfileResponse>(ErrorCodes.CreateUserReloadFailed);
        }

        // Only an administrator creates accounts, so no tokens are issued here: they would sign the administrator
        // in as the new user. The new user signs in with the password they are given.
        var roles = reloaded.UserRoles.Select(ur => ur.Role.Code).ToList();
        return Result.Success(UserProfileMapper.Map(reloaded, roles));
    }
}
