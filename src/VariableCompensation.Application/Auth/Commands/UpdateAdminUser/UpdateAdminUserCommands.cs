using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth;
using VariableCompensation.Application.Auth.Commands.RegisterUser;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Application.Common;
using VariableCompensation.Application.Hr.EvaluatorSettings.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Auth.Commands.UpdateAdminUser;

public sealed record UpdateAdminUserCommand(
    long UserId,
    string Email,
    bool IsActive,
    IReadOnlyList<string> RoleCodes,
    long? ControllerEmployeeId,
    int? Version)
    : IRequest<Result<AdminUserListItemResponse>>;

public sealed class UpdateAdminUserCommandHandler : IRequestHandler<UpdateAdminUserCommand, Result<AdminUserListItemResponse>>
{
    private readonly IUserRepository userRepository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository;
    private readonly IRoleLookup roleLookup;

    public UpdateAdminUserCommandHandler(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        IRoleLookup roleLookup)
    {
        this.userRepository = userRepository;
        this.employeeRepository = employeeRepository;
        this.evaluatorSettingsRepository = evaluatorSettingsRepository;
        this.roleLookup = roleLookup;
    }

    public async Task<Result<AdminUserListItemResponse>> Handle(
        UpdateAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Result.Failure<AdminUserListItemResponse>(ErrorCodes.EmailInvalid);
        }

        var user = await this.userRepository.FindByIdForUpdateAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AdminUserListItemResponse>(ErrorCodes.UserNotFound);
        }

        var version = EditVersion.Claim(user, request.Version);
        if (version.IsFailure)
        {
            return Result.Failure<AdminUserListItemResponse>(version.Error);
        }

        if (await this.userRepository.EmailExistsAsync(email, request.UserId, cancellationToken))
        {
            return Result.Failure<AdminUserListItemResponse>(ErrorCodes.EmailAlreadyRegistered);
        }

        var roleIdsResult = await UserRoleSync.ResolveRoleIdsAsync(request.RoleCodes, this.roleLookup, cancellationToken);
        if (roleIdsResult.IsFailure)
        {
            return Result.Failure<AdminUserListItemResponse>(roleIdsResult.Error);
        }

        // Without an active administrator nobody can manage accounts any more, and nobody could restore one.
        var isActiveAdministrator = user.IsActive && user.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Admin);
        var staysActiveAdministrator = request.IsActive
            && request.RoleCodes.Any(code => string.Equals(code.Trim(), RoleCodes.Admin, StringComparison.OrdinalIgnoreCase));
        if (isActiveAdministrator && !staysActiveAdministrator
            && !await this.userRepository.HasOtherActiveUserInRoleAsync(RoleCodes.Admin, user.Id, cancellationToken))
        {
            return Result.Failure<AdminUserListItemResponse>(ErrorCodes.LastActiveAdministrator);
        }

        // A role that acts as an employee is only given to an account linked to one. Roles it already holds from
        // before this rule are left alone.
        var heldRoles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var addedRoles = request.RoleCodes
            .Where(code => !heldRoles.Contains(code.Trim(), StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (EmployeeLinkedRoles.AnyIn(addedRoles)
            && await this.employeeRepository.FindByUserIdAsync(user.Id, cancellationToken) is null)
        {
            return Result.Failure<AdminUserListItemResponse>(ErrorCodes.EmployeeRequiredForRoles);
        }

        var controllerRoleId = await this.roleLookup.FindRoleIdByCodeAsync(RoleCodes.Controller, cancellationToken);
        var hadControllerRole = controllerRoleId is not null
            && user.UserRoles.Any(ur => ur.RoleId == controllerRoleId.Value);
        var keepsControllerRole = request.RoleCodes.Contains(RoleCodes.Controller, StringComparer.OrdinalIgnoreCase);

        if (hadControllerRole && !keepsControllerRole)
        {
            var controllerRemoval = await ControllerRoleCheck.EnsureRoleCanBeRemovedAsync(
                user.Id,
                this.employeeRepository,
                this.evaluatorSettingsRepository,
                cancellationToken);
            if (controllerRemoval.IsFailure)
            {
                return Result.Failure<AdminUserListItemResponse>(controllerRemoval.Error);
            }
        }

        var evaluatorSync = await EvaluatorRoleSync.ApplyAsync(
            user.Id,
            request.RoleCodes.Contains(RoleCodes.Evaluator, StringComparer.OrdinalIgnoreCase),
            request.ControllerEmployeeId,
            this.employeeRepository,
            this.evaluatorSettingsRepository,
            this.userRepository,
            cancellationToken);
        if (evaluatorSync.IsFailure)
        {
            return Result.Failure<AdminUserListItemResponse>(evaluatorSync.Error);
        }

        var previousRoleIds = user.UserRoles.Select(ur => ur.RoleId).OrderBy(id => id).ToList();
        var wasActive = user.IsActive;
        user.Email = email;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        UserRoleSync.ApplyRoles(user, roleIdsResult.Value);

        var updatedRoleIds = roleIdsResult.Value.OrderBy(id => id).ToList();
        var rolesChanged = !previousRoleIds.SequenceEqual(updatedRoleIds);

        // A deactivated account must not come back signed in if it is ever reactivated.
        var deactivated = wasActive && !user.IsActive;
        if (rolesChanged || deactivated)
        {
            await this.userRepository.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        }

        await this.userRepository.SaveChangesAsync(cancellationToken);
        await this.evaluatorSettingsRepository.SaveChangesAsync(cancellationToken);

        var reloaded = await this.userRepository.FindByIdWithRolesAsync(user.Id, cancellationToken);
        if (reloaded is null)
        {
            return Result.Failure<AdminUserListItemResponse>(ErrorCodes.UserNotFound);
        }

        return Result.Success(await AdminUserMapper.MapAsync(reloaded, this.employeeRepository, cancellationToken));
    }
}

public sealed record ResetAdminUserPasswordCommand(long UserId, string NewPassword) : IRequest<Result>;

public sealed class ResetAdminUserPasswordCommandHandler : IRequestHandler<ResetAdminUserPasswordCommand, Result>
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;

    public ResetAdminUserPasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ResetAdminUserPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword.Length < 8)
        {
            return Result.Failure(ErrorCodes.PasswordTooShort);
        }

        var user = await this.userRepository.FindByIdForUpdateAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(ErrorCodes.UserNotFound);
        }

        user.PasswordHash = this.passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await this.userRepository.RevokeAllRefreshTokensAsync(request.UserId, cancellationToken);
        await this.userRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal static class AdminUserMapper
{
    internal static async Task<AdminUserListItemResponse> MapAsync(
        Domain.Entities.Identity.User user,
        IEmployeeRepository employeeRepository,
        CancellationToken cancellationToken)
    {
        var employeesByUserId = await employeeRepository.GetEmployeesByUserIdsAsync(cancellationToken);
        employeesByUserId.TryGetValue(user.Id, out var employee);

        return new AdminUserListItemResponse
        {
            Id = user.Id,
            Version = user.Version,
            Email = user.Email,
            Roles = user.UserRoles.Select(ur => ur.Role.Code).OrderBy(code => code).ToList(),
            IsActive = user.IsActive,
            EmployeeId = employee?.Id,
            EmployeeFullName = employee?.FullName,
        };
    }
}
