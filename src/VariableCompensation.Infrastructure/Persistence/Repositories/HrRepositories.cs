using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Infrastructure.Persistence.Repositories;

public sealed class OrganizationUnitRepository : IOrganizationUnitRepository
{
    private readonly AppDbContext context;

    public OrganizationUnitRepository(AppDbContext context) => this.context = context;

    public Task<OrganizationUnit?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        this.context.OrganizationUnits.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken cancellationToken) =>
        this.context.OrganizationUnits.AnyAsync(
            x => x.Name == name && (excludeId == null || x.Id != excludeId),
            cancellationToken);

    public async Task<IReadOnlyList<OrganizationUnit>> GetAllAsync(bool? isActive, CancellationToken cancellationToken) =>
        await this.context.OrganizationUnits
            .AsNoTracking()
            .Where(x => isActive == null || x.IsActive == isActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(OrganizationUnit entity, CancellationToken cancellationToken) =>
        await this.context.OrganizationUnits.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);
}

public sealed class JobPositionRepository : IJobPositionRepository
{
    private readonly AppDbContext context;

    public JobPositionRepository(AppDbContext context) => this.context = context;

    public Task<JobPosition?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        this.context.JobPositions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken cancellationToken) =>
        this.context.JobPositions.AnyAsync(
            x => x.Name == name && (excludeId == null || x.Id != excludeId),
            cancellationToken);

    public async Task<IReadOnlyList<JobPosition>> GetAllAsync(bool? isActive, CancellationToken cancellationToken) =>
        await this.context.JobPositions
            .AsNoTracking()
            .Where(x => isActive == null || x.IsActive == isActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(JobPosition entity, CancellationToken cancellationToken) =>
        await this.context.JobPositions.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);
}

public sealed class EducationLevelRepository : IEducationLevelRepository
{
    private readonly AppDbContext context;

    public EducationLevelRepository(AppDbContext context) => this.context = context;

    public Task<EducationLevel?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        this.context.EducationLevels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken cancellationToken) =>
        this.context.EducationLevels.AnyAsync(
            x => x.Name == name && (excludeId == null || x.Id != excludeId),
            cancellationToken);

    public async Task<IReadOnlyList<EducationLevel>> GetAllAsync(bool? isActive, CancellationToken cancellationToken) =>
        await this.context.EducationLevels
            .AsNoTracking()
            .Where(x => isActive == null || x.IsActive == isActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(EducationLevel entity, CancellationToken cancellationToken) =>
        await this.context.EducationLevels.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);
}

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext context;

    public EmployeeRepository(AppDbContext context) => this.context = context;

    public Task<Employee?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        this.BuildEmployeeQuery(asNoTracking: true)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Employee?> FindByIdForUpdateAsync(long id, CancellationToken cancellationToken) =>
        this.BuildEmployeeQuery(asNoTracking: false)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    private IQueryable<Employee> BuildEmployeeQuery(bool asNoTracking)
    {
        var query = this.context.Employees
            .Include(e => e.OrganizationUnit)
            .Include(e => e.JobPosition)
            .Include(e => e.EducationLevel)
            .Include(e => e.Evaluator)
            .AsQueryable();

        return asNoTracking ? query.AsNoTracking() : query;
    }

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
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
        CancellationToken cancellationToken)
    {
        var query = this.context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.JobPosition)
            .Include(e => e.EducationLevel)
            .Include(e => e.Evaluator)
            .AsQueryable();

        if (organizationUnitId is not null)
        {
            query = query.Where(e => e.OrganizationUnitId == organizationUnitId);
        }

        if (evaluatorEmployeeId is not null)
        {
            if (evaluatorEmployeeId == -1)
            {
                return (Array.Empty<Employee>(), 0);
            }

            query = query.Where(e => e.EvaluatorEmployeeId == evaluatorEmployeeId);
        }

        if (controllerEmployeeId is not null)
        {
            if (controllerEmployeeId == -1)
            {
                return (Array.Empty<Employee>(), 0);
            }

            query = query.Where(e =>
                e.EvaluatorEmployeeId != null &&
                this.context.EvaluatorSettings.Any(s =>
                    s.EmployeeId == e.EvaluatorEmployeeId.Value &&
                    s.ControllerEmployeeId == controllerEmployeeId));
        }

        if (isActive is not null)
        {
            query = query.Where(e => e.IsActive == isActive);
        }

        query = EmployeeNameSearch.Apply(query, search);

        if (!string.IsNullOrWhiteSpace(goalsBucket)
            && EmployeeGoalsFilter.TryParseGoalsBucket(goalsBucket, out var normalizedGoalsBucket)
            && normalizedGoalsBucket == "pending"
            && goalsYear is not null
            && goalsQuarter is not null)
        {
            query = EmployeeGoalsFilter.ApplyPendingForPeriod(
                query,
                this.context.Evaluations.AsQueryable(),
                goalsYear.Value,
                goalsQuarter.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetWithoutCurrentSalaryPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = this.context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Where(e => !this.context.EmployeeSalaries.Any(s => s.EmployeeId == e.Id && s.EffectiveTo == null))
            .AsQueryable();

        var term = EmployeeNameSearch.Normalize(search);
        if (term is not null)
        {
            query = query.Where(e =>
                (e.FirstName + " " + e.LastName).ToLower().Contains(term) ||
                (e.LastName + " " + e.FirstName).ToLower().Contains(term) ||
                (e.OrganizationUnit != null && e.OrganizationUnit.Name.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken) =>
        this.context.Employees.AnyAsync(e => e.Id == id, cancellationToken);

    public Task<Employee?> FindByUserIdAsync(long userId, CancellationToken cancellationToken) =>
        this.context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public Task<bool> IsUserLinkedToAnotherEmployeeAsync(long userId, long excludeEmployeeId, CancellationToken cancellationToken) =>
        this.context.Employees.AnyAsync(e => e.UserId == userId && e.Id != excludeEmployeeId, cancellationToken);

    public Task<Employee?> FindByIdWithEvaluatorAsync(long id, CancellationToken cancellationToken) =>
        this.context.Employees
            .AsNoTracking()
            .Include(e => e.Evaluator)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<long, Employee>> GetEmployeesByUserIdsAsync(CancellationToken cancellationToken)
    {
        var employees = await this.context.Employees
            .AsNoTracking()
            .Where(e => e.UserId != null)
            .ToListAsync(cancellationToken);

        return employees
            .Where(e => e.UserId.HasValue)
            .ToDictionary(e => e.UserId!.Value);
    }

    public async Task AddAsync(Employee entity, CancellationToken cancellationToken) =>
        await this.context.Employees.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);
}
