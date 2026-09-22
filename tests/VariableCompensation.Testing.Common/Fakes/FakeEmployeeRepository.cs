using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeEmployeeRepository : IEmployeeRepository
{
    public Dictionary<long, Employee> EmployeesByUserId { get; } = [];

    public HashSet<long> ExistingEmployeeIds { get; } = [];

    public HashSet<long> EvaluatorsWithSubordinates { get; } = [];

    public Task<Employee?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult<Employee?>(null);

    public Task<Employee?> FindByIdForUpdateAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult<Employee?>(null);

    public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        long? organizationUnitId,
        long? evaluatorEmployeeId,
        string? search,
        bool? isActive,
        long? controllerEmployeeId,
        short? goalsYear,
        byte? goalsQuarter,
        string? goalsBucket,
        CancellationToken cancellationToken) =>
        Task.FromResult<(IReadOnlyList<Employee> Items, int TotalCount)>(([], 0));

    public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetWithoutCurrentSalaryPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken) =>
        Task.FromResult<(IReadOnlyList<Employee> Items, int TotalCount)>(([], 0));

    public Task<Employee?> FindByUserIdAsync(long userId, CancellationToken cancellationToken) =>
        Task.FromResult(this.EmployeesByUserId.GetValueOrDefault(userId));

    public Task<bool> IsUserLinkedToAnotherEmployeeAsync(long userId, long excludeEmployeeId, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(this.ExistingEmployeeIds.Contains(id));

    public Task<bool> HasSubordinatesAsync(long evaluatorEmployeeId, CancellationToken cancellationToken) =>
        Task.FromResult(this.EvaluatorsWithSubordinates.Contains(evaluatorEmployeeId));

    public Task<Employee?> FindByIdWithEvaluatorAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult<Employee?>(null);

    public Task<IReadOnlyDictionary<long, Employee>> GetEmployeesByUserIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<long, Employee>>(new Dictionary<long, Employee>());

    public Task AddAsync(Employee entity, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
