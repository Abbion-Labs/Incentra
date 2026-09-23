using CSharpFunctionalExtensions;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain;
using EvaluatorSettingsEntity = VariableCompensation.Domain.Entities.Hr.EvaluatorSettings;

namespace VariableCompensation.Application.Hr.EvaluatorSettings.Services;

/// <summary>
/// Keeps the EVALUATOR role and the evaluator settings row in step, so that
/// granting the role is the single act that makes someone an evaluator.
/// Without this the two drift apart: a person could be assigned as someone's
/// evaluator while having no way to sign in and rate them.
/// </summary>
public static class EvaluatorRoleSync
{
    public static async Task<Result> ApplyAsync(
        long userId,
        bool hasEvaluatorRole,
        long? controllerEmployeeId,
        IEmployeeRepository employeeRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.FindByUserIdAsync(userId, cancellationToken);

        if (!hasEvaluatorRole)
        {
            return employee is null
                ? Result.Success()
                : await RemoveSettingsAsync(employee.Id, employeeRepository, evaluatorSettingsRepository, cancellationToken);
        }

        if (employee is null)
        {
            return Result.Failure(ErrorCodes.EvaluatorUserNotLinkedToEmployee);
        }

        // Already an evaluator: keep the controller the admin has assigned.
        if (await evaluatorSettingsRepository.ExistsAsync(employee.Id, cancellationToken))
        {
            return Result.Success();
        }

        if (controllerEmployeeId is null)
        {
            return Result.Failure(ErrorCodes.EvaluatorControllerRequired);
        }

        var controllerCheck = await ControllerRoleCheck.EnsureIsAControllerAsync(
            controllerEmployeeId.Value,
            employeeRepository,
            userRepository,
            cancellationToken);
        if (controllerCheck.IsFailure)
        {
            return controllerCheck;
        }

        await evaluatorSettingsRepository.AddAsync(
            new EvaluatorSettingsEntity
            {
                EmployeeId = employee.Id,
                ControllerEmployeeId = controllerEmployeeId.Value,
            },
            cancellationToken);

        return Result.Success();
    }

    private static async Task<Result> RemoveSettingsAsync(
        long employeeId,
        IEmployeeRepository employeeRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        CancellationToken cancellationToken)
    {
        var settings = await evaluatorSettingsRepository.FindByEmployeeIdForUpdateAsync(employeeId, cancellationToken);
        if (settings is null)
        {
            return Result.Success();
        }

        // Dropping the settings while people still point at this evaluator would
        // leave them with nobody able to rate them.
        if (await employeeRepository.HasSubordinatesAsync(employeeId, cancellationToken))
        {
            return Result.Failure(ErrorCodes.EvaluatorHasSubordinates);
        }

        evaluatorSettingsRepository.Remove(settings);
        return Result.Success();
    }
}
