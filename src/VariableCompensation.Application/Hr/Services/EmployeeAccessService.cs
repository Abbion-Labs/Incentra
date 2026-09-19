using CSharpFunctionalExtensions;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Hr.Services;

public sealed class EmployeeAccessService
{
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;
    private readonly ControllerSupervisionService controllerSupervisionService;

    public EmployeeAccessService(
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext,
        ControllerSupervisionService controllerSupervisionService)
    {
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
        this.controllerSupervisionService = controllerSupervisionService;
    }

    public async Task<Result> EnsureCanViewAsync(Employee employee, CancellationToken cancellationToken)
    {
        if (this.currentUserService.IsAdmin)
        {
            return Result.Success();
        }

        var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(ErrorCodes.UserNotLinkedToEmployee);
        }

        if (employee.Id == currentEmployeeId)
        {
            return Result.Success();
        }

        if (this.currentUserService.IsInRole(RoleCodes.Evaluator) &&
            employee.EvaluatorEmployeeId == currentEmployeeId)
        {
            return Result.Success();
        }

        if (this.currentUserService.IsInRole(RoleCodes.Controller) &&
            await this.controllerSupervisionService.SupervisesEmployeeAsync(currentEmployeeId.Value, employee.Id, cancellationToken))
        {
            return Result.Success();
        }

        return Result.Failure(ErrorCodes.EmployeeAccessDenied);
    }
}
