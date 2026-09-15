using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Compensation.Services;

public sealed class CompensationAccessService
{
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository;

    public CompensationAccessService(
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext,
        IEvaluatorSettingsRepository evaluatorSettingsRepository)
    {
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
        this.evaluatorSettingsRepository = evaluatorSettingsRepository;
    }

    public async Task<(long? EmployeeId, long? OrganizationUnitId)> ResolveResultListFiltersAsync(
        long? employeeId,
        long? organizationUnitId,
        CancellationToken cancellationToken)
    {
        if (this.currentUserService.IsAdmin || this.currentUserService.IsInRole(RoleCodes.Payroll))
        {
            return (employeeId, organizationUnitId);
        }

        var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return (-1, organizationUnitId);
        }

        if (this.currentUserService.IsInRole(RoleCodes.Employee))
        {
            return (currentEmployeeId, null);
        }

        return (employeeId, organizationUnitId);
    }

    public async Task<bool> CanViewResultAsync(VariableCompensationResult result, CancellationToken cancellationToken)
    {
        if (this.currentUserService.IsAdmin || this.currentUserService.IsInRole(RoleCodes.Payroll))
        {
            return true;
        }

        var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return false;
        }

        if (result.EmployeeId == currentEmployeeId)
        {
            return true;
        }

        if (this.currentUserService.IsInRole(RoleCodes.Evaluator) &&
            result.Employee.EvaluatorEmployeeId == currentEmployeeId)
        {
            return true;
        }

        if (this.currentUserService.IsInRole(RoleCodes.Controller) &&
            result.Employee.EvaluatorEmployeeId is not null)
        {
            var settings = await this.evaluatorSettingsRepository.FindByEmployeeIdAsync(
                result.Employee.EvaluatorEmployeeId.Value,
                cancellationToken);

            if (settings?.ControllerEmployeeId == currentEmployeeId)
            {
                return true;
            }
        }

        return false;
    }
}
