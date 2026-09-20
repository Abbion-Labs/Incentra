using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeEvaluatorSettingsRepository : IEvaluatorSettingsRepository
{
    public Dictionary<long, EvaluatorSettings> Store { get; } = [];

    public Task<EvaluatorSettings?> FindByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(this.Store.GetValueOrDefault(employeeId));

    public Task<EvaluatorSettings?> FindByEmployeeIdForUpdateAsync(long employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(this.Store.GetValueOrDefault(employeeId));

    public Task<IReadOnlyList<EvaluatorSettings>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EvaluatorSettings>>(this.Store.Values.ToList());

    public Task<IReadOnlyList<EvaluatorSettings>> GetByControllerEmployeeIdAsync(
        long controllerEmployeeId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EvaluatorSettings>>(
            this.Store.Values.Where(s => s.ControllerEmployeeId == controllerEmployeeId).ToList());

    public Task<bool> ExistsAsync(long employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(this.Store.ContainsKey(employeeId));

    public Task AddAsync(EvaluatorSettings entity, CancellationToken cancellationToken)
    {
        this.Store[entity.EmployeeId] = entity;
        return Task.CompletedTask;
    }

    public void Remove(EvaluatorSettings entity) => this.Store.Remove(entity.EmployeeId);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
