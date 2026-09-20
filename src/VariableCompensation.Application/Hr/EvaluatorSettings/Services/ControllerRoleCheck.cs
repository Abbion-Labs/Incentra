using CSharpFunctionalExtensions;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Hr.EvaluatorSettings.Services;

/// <summary>
/// A controller is nothing but an account carrying the CONTROLLER role: there
/// is no other record of one. Someone named as a controller without it cannot
/// open the screens that review their evaluators, so the assignment would look
/// right and do nothing.
/// </summary>
public static class ControllerRoleCheck
{
    public static async Task<Result> EnsureIsAControllerAsync(
        long controllerEmployeeId,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        if (!await employeeRepository.ExistsAsync(controllerEmployeeId, cancellationToken))
        {
            return Result.Failure(ErrorCodes.ControllerNotFound);
        }

        var user = await userRepository.FindByEmployeeIdAsync(controllerEmployeeId, cancellationToken);
        var isController = user?.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Controller) ?? false;

        return isController
            ? Result.Success()
            : Result.Failure(ErrorCodes.ControllerRoleRequired);
    }

    /// <summary>
    /// Taking the role away from someone who still supervises evaluators would
    /// leave those evaluators with a controller who can no longer review them.
    /// </summary>
    public static async Task<Result> EnsureRoleCanBeRemovedAsync(
        long userId,
        IEmployeeRepository employeeRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.FindByUserIdAsync(userId, cancellationToken);
        if (employee is null)
        {
            return Result.Success();
        }

        return await evaluatorSettingsRepository.IsControllerForAnyEvaluatorAsync(employee.Id, cancellationToken)
            ? Result.Failure(ErrorCodes.ControllerHasEvaluators)
            : Result.Success();
    }
}
