using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Infrastructure.Persistence;

namespace VariableCompensation.Infrastructure.Persistence.Repositories;

public sealed class EmployeeSalaryRepository : IEmployeeSalaryRepository
{
    private readonly AppDbContext context;

    public EmployeeSalaryRepository(AppDbContext context) => this.context = context;

    public Task<EmployeeSalary?> FindCurrentByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken) =>
        this.context.EmployeeSalaries
            .AsNoTracking()
            .Include(s => s.Employee)
            .ThenInclude(e => e.OrganizationUnit)
            .Where(s => s.EmployeeId == employeeId && s.EffectiveTo == null)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<EmployeeSalary?> FindCurrentByEmployeeIdForUpdateAsync(long employeeId, CancellationToken cancellationToken) =>
        this.context.EmployeeSalaries
            .Where(s => s.EmployeeId == employeeId && s.EffectiveTo == null)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<long, EmployeeSalary>> GetEffectiveByEmployeeIdsAsync(
        IReadOnlyCollection<long> employeeIds,
        DateOnly asOfDate,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return new Dictionary<long, EmployeeSalary>();
        }

        var rows = await this.context.EmployeeSalaries
            .AsNoTracking()
            .Where(s =>
                employeeIds.Contains(s.EmployeeId)
                && s.EffectiveFrom <= asOfDate
                && (s.EffectiveTo == null || s.EffectiveTo >= asOfDate))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(s => s.EffectiveFrom).First());
    }

    public async Task<IReadOnlyList<EmployeeSalary>> GetAllCurrentWithEmployeesAsync(CancellationToken cancellationToken) =>
        await this.context.EmployeeSalaries
            .AsNoTracking()
            .Include(s => s.Employee)
            .ThenInclude(e => e.OrganizationUnit)
            .Where(s => s.EffectiveTo == null)
            .OrderBy(s => s.Employee.LastName)
            .ThenBy(s => s.Employee.FirstName)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<EmployeeSalary> Items, int TotalCount)> GetCurrentPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = this.context.EmployeeSalaries
            .AsNoTracking()
            .Include(s => s.Employee)
            .ThenInclude(e => e.OrganizationUnit)
            .Where(s => s.EffectiveTo == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = EmployeeNameSearch.Normalize(search)!;
            query = query.Where(s =>
                (s.Employee.FirstName + " " + s.Employee.LastName).ToLower().Contains(term) ||
                (s.Employee.LastName + " " + s.Employee.FirstName).ToLower().Contains(term) ||
                (s.Employee.OrganizationUnit != null && s.Employee.OrganizationUnit.Name.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.Employee.LastName)
            .ThenBy(s => s.Employee.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<EmployeeSalary>> GetHistoryByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken) =>
        await this.context.EmployeeSalaries
            .AsNoTracking()
            .Include(s => s.Employee)
            .ThenInclude(e => e.OrganizationUnit)
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<long>> GetEmployeeIdsWithCurrentSalaryAsync(CancellationToken cancellationToken)
    {
        var ids = await this.context.EmployeeSalaries
            .AsNoTracking()
            .Where(s => s.EffectiveTo == null)
            .Select(s => s.EmployeeId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task AddAsync(EmployeeSalary entity, CancellationToken cancellationToken) =>
        await this.context.EmployeeSalaries.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);
}
