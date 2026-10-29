using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Application.Hr.EvaluatorSettings.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;
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
    private readonly IEmployeeRepository employeeRepository;
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRoleLookup roleLookup,
        IPasswordHasher passwordHasher,
        IEmployeeRepository employeeRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository)
    {
        this.userRepository = userRepository;
        this.roleLookup = roleLookup;
        this.passwordHasher = passwordHasher;
        this.employeeRepository = employeeRepository;
        this.evaluatorSettingsRepository = evaluatorSettingsRepository;
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

        var roleIdsResult = await UserRoleSync.ResolveRoleIdsAsync(request.RoleCodes, this.roleLookup, cancellationToken);
        if (roleIdsResult.IsFailure)
        {
            return Result.Failure<UserProfileResponse>(roleIdsResult.Error);
        }

        var employee = await this.FindEmployeeToLinkAsync(request.EmployeeId, cancellationToken);
        if (employee.IsFailure)
        {
            return Result.Failure<UserProfileResponse>(employee.Error);
        }

        // An employee, an evaluator and a controller each act as a particular employee, from the first sign-in.
        if (employee.Value is null && EmployeeLinkedRoles.AnyIn(request.RoleCodes))
        {
            return Result.Failure<UserProfileResponse>(ErrorCodes.EmployeeRequiredForRoles);
        }

        var evaluatorSync = await EvaluatorRoleSync.ApplyToEmployeeAsync(
            employee.Value,
            request.RoleCodes.Contains(RoleCodes.Evaluator, StringComparer.OrdinalIgnoreCase),
            request.ControllerEmployeeId,
            this.employeeRepository,
            this.evaluatorSettingsRepository,
            this.userRepository,
            cancellationToken);
        if (evaluatorSync.IsFailure)
        {
            return Result.Failure<UserProfileResponse>(evaluatorSync.Error);
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

        if (employee.Value is not null)
        {
            employee.Value.User = user;
            employee.Value.UpdatedAt = DateTime.UtcNow;

            // A form still open on the employee has not seen the account.
            employee.Value.Version++;
        }

        // The account, its link and any evaluator settings are saved together.
        await this.userRepository.SaveChangesAsync(cancellationToken);

        var reloaded = await this.userRepository.FindByIdWithRolesAsync(user.Id, cancellationToken);
        if (reloaded is null)
        {
            return Result.Failure<UserProfileResponse>(ErrorCodes.CreateUserReloadFailed);
        }

        // Only an administrator creates accounts, so no tokens are issued here: they would sign the administrator
        // in as the new user. The new user signs in with the password they are given.
        var roles = reloaded.UserRoles.Select(ur => ur.Role.Code).ToList();
        var linkedEmployee = await this.employeeRepository.FindByUserIdAsync(user.Id, cancellationToken);
        return Result.Success(UserProfileMapper.Map(reloaded, roles, linkedEmployee));
    }

    private async Task<Result<Employee?>> FindEmployeeToLinkAsync(long? employeeId, CancellationToken cancellationToken)
    {
        if (employeeId is null)
        {
            return Result.Success<Employee?>(null);
        }

        var employee = await this.employeeRepository.FindByIdForUpdateAsync(employeeId.Value, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<Employee?>(ErrorCodes.EmployeeNotFound);
        }

        if (!employee.IsActive)
        {
            return Result.Failure<Employee?>(ErrorCodes.EmployeeInactive);
        }

        return employee.UserId is null
            ? Result.Success<Employee?>(employee)
            : Result.Failure<Employee?>(ErrorCodes.EmployeeAlreadyHasAccount);
    }
}
