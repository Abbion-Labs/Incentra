using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Abstractions.Persistence;

public interface IEmployeeSalaryRepository
{
    Task<EmployeeSalary?> FindCurrentByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken);

    Task<EmployeeSalary?> FindCurrentByEmployeeIdForUpdateAsync(long employeeId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<long, EmployeeSalary>> GetEffectiveByEmployeeIdsAsync(
        IReadOnlyCollection<long> employeeIds,
        DateOnly asOfDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeSalary>> GetAllCurrentWithEmployeesAsync(CancellationToken cancellationToken);

    Task<(IReadOnlyList<EmployeeSalary> Items, int TotalCount)> GetCurrentPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeSalary>> GetHistoryByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken);

    Task<IReadOnlySet<long>> GetEmployeeIdsWithCurrentSalaryAsync(CancellationToken cancellationToken);

    Task AddAsync(EmployeeSalary entity, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
