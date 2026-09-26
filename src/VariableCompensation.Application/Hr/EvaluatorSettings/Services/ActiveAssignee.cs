using VariableCompensation.Application.Abstractions.Persistence;

namespace VariableCompensation.Application.Hr.EvaluatorSettings.Services;

/// <summary>
/// An evaluator or a controller acts through their account. Someone who has left, or whose account is closed,
/// can no longer sign in to rate or review, so they are not given anyone new.
/// </summary>
public static class ActiveAssignee
{
    public static async Task<bool> CanSignInAsync(
        long employeeId,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.FindByIdAsync(employeeId, cancellationToken);
        if (employee is null || !employee.IsActive)
        {
            return false;
        }

        var user = await userRepository.FindByEmployeeIdAsync(employeeId, cancellationToken);
        return user?.IsActive == true;
    }
}
