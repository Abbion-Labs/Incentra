using CSharpFunctionalExtensions;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Hr.Employees.Commands;

internal static class EmployeeAvatarAccess
{
    internal static async Task<Result> EnsureCanManageAsync(
        Domain.Entities.Hr.Employee employee,
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext,
        CancellationToken cancellationToken)
    {
        if (currentUserService.IsAdmin)
        {
            return Result.Success();
        }

        var currentEmployeeId = await currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(ErrorCodes.UserNotLinkedToEmployee);
        }

        if (employee.Id == currentEmployeeId)
        {
            return Result.Success();
        }

        if (currentUserService.IsInRole(RoleCodes.Evaluator) && employee.EvaluatorEmployeeId == currentEmployeeId)
        {
            return Result.Success();
        }

        return Result.Failure(ErrorCodes.Forbidden);
    }
}
