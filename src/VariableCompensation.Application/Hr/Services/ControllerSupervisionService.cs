using VariableCompensation.Application.Abstractions.Persistence;

namespace VariableCompensation.Application.Hr.Services;

public sealed class ControllerSupervisionService
{
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository;
    private readonly IEmployeeRepository employeeRepository;

    public ControllerSupervisionService(
        IEvaluatorSettingsRepository evaluatorSettingsRepository,
        IEmployeeRepository employeeRepository)
    {
        this.evaluatorSettingsRepository = evaluatorSettingsRepository;
        this.employeeRepository = employeeRepository;
    }

    public async Task<bool> SupervisesEvaluatorAsync(
        long controllerEmployeeId,
        long evaluatorEmployeeId,
        CancellationToken cancellationToken)
    {
        var settings = await this.evaluatorSettingsRepository.FindByEmployeeIdAsync(evaluatorEmployeeId, cancellationToken);
        return settings?.ControllerEmployeeId == controllerEmployeeId;
    }

    public async Task<bool> SupervisesEmployeeAsync(
        long controllerEmployeeId,
        long employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await this.employeeRepository.FindByIdWithEvaluatorAsync(employeeId, cancellationToken);
        if (employee?.EvaluatorEmployeeId is null)
        {
            return false;
        }

        return await this.SupervisesEvaluatorAsync(controllerEmployeeId, employee.EvaluatorEmployeeId.Value, cancellationToken);
    }
}
