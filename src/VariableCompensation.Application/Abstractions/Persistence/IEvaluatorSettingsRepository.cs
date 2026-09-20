using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Abstractions.Persistence;

public interface IEvaluatorSettingsRepository
{
    Task<EvaluatorSettings?> FindByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken);

    Task<EvaluatorSettings?> FindByEmployeeIdForUpdateAsync(long employeeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EvaluatorSettings>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<EvaluatorSettings>> GetByControllerEmployeeIdAsync(
        long controllerEmployeeId,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(long employeeId, CancellationToken cancellationToken);

    Task AddAsync(EvaluatorSettings entity, CancellationToken cancellationToken);

    void Remove(EvaluatorSettings entity);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
