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
    // Mirrors the defaults the admin form has always pre-filled, so that
    // granting the role produces the same settings an admin would have typed.
    public const decimal DefaultThresholdDoesNotMeet = 2m;
    public const decimal DefaultThresholdMeets = 2.5m;
    public const decimal DefaultThresholdGood = 3.5m;
    public const decimal DefaultThresholdExceeds = 4.5m;
    public const decimal DefaultPercentDoesNotMeet = 0m;
    public const decimal DefaultPercentMeets = 25m;
    public const decimal DefaultPercentGood = 50m;
    public const decimal DefaultPercentExceeds = 100m;

    public static async Task<Result> ApplyAsync(
        long userId,
        bool hasEvaluatorRole,
        long? controllerEmployeeId,
        IEmployeeRepository employeeRepository,
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
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

        // Already an evaluator: leave the thresholds the admin has tuned alone.
        if (await evaluatorSettingsRepository.ExistsAsync(employee.Id, cancellationToken))
        {
            return Result.Success();
        }

        if (controllerEmployeeId is null)
        {
            return Result.Failure(ErrorCodes.EvaluatorControllerRequired);
        }

        if (!await employeeRepository.ExistsAsync(controllerEmployeeId.Value, cancellationToken))
        {
            return Result.Failure(ErrorCodes.ControllerNotFound);
        }

        await evaluatorSettingsRepository.AddAsync(
            new EvaluatorSettingsEntity
            {
                EmployeeId = employee.Id,
                ControllerEmployeeId = controllerEmployeeId.Value,
                ThresholdDoesNotMeet = DefaultThresholdDoesNotMeet,
                ThresholdMeets = DefaultThresholdMeets,
                ThresholdGood = DefaultThresholdGood,
                ThresholdExceeds = DefaultThresholdExceeds,
                PercentDoesNotMeet = DefaultPercentDoesNotMeet,
                PercentMeets = DefaultPercentMeets,
                PercentGood = DefaultPercentGood,
                PercentExceeds = DefaultPercentExceeds,
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
