using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Compensation.Models;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Infrastructure.Persistence.Repositories;

public sealed class CompensationRepository : ICompensationRepository
{
    private readonly AppDbContext context;

    public CompensationRepository(AppDbContext context) => this.context = context;

    public Task<VariableCompensationParameters?> FindParametersByIdAsync(long id, CancellationToken cancellationToken) =>
        this.context.VariableCompensationParameters
            .AsNoTracking()
            .Include(p => p.OrganizationUnit)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<VariableCompensationParameters?> FindParametersByIdForUpdateAsync(long id, CancellationToken cancellationToken) =>
        this.context.VariableCompensationParameters
            .Include(p => p.OrganizationUnit)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VariableCompensationParameters>> GetParametersAsync(
        long? organizationUnitId,
        short? year,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = this.context.VariableCompensationParameters
            .AsNoTracking()
            .Include(p => p.OrganizationUnit)
            .AsQueryable();

        if (organizationUnitId is not null)
        {
            query = query.Where(p => p.OrganizationUnitId == organizationUnitId);
        }

        if (year is not null)
        {
            query = query.Where(p => p.Year == year);
        }

        if (isActive is not null)
        {
            query = query.Where(p => p.IsActive == isActive);
        }

        return await query.OrderByDescending(p => p.Year).ThenBy(p => p.OrganizationUnit.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ParametersExistsForOrgUnitYearAsync(
        long organizationUnitId,
        short year,
        long? excludeId,
        CancellationToken cancellationToken) =>
        this.context.VariableCompensationParameters.AnyAsync(
            p => p.OrganizationUnitId == organizationUnitId &&
                 p.Year == year &&
                 (excludeId == null || p.Id != excludeId),
            cancellationToken);

    public async Task AddParametersAsync(VariableCompensationParameters entity, CancellationToken cancellationToken) =>
        await this.context.VariableCompensationParameters.AddAsync(entity, cancellationToken);

    public Task<VariableCompensationResult?> FindResultByIdAsync(long id, CancellationToken cancellationToken) =>
        this.context.VariableCompensationResults
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.OrganizationUnit)
            .Include(r => r.Employee).ThenInclude(e => e.JobPosition)
            .Include(r => r.Parameters).ThenInclude(p => p.OrganizationUnit)
            .Include(r => r.EvaluationLinks).ThenInclude(l => l.Evaluation)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<VariableCompensationResult> Items, int TotalCount)> GetResultsPagedAsync(
        int page,
        int pageSize,
        long? parametersId,
        short? year,
        long? organizationUnitId,
        long? employeeId,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = this.context.VariableCompensationResults
            .AsNoTracking()
            .AsQueryable();

        if (parametersId is not null)
        {
            query = query.Where(r => r.ParametersId == parametersId);
        }

        if (year is not null)
        {
            query = query.Where(r => r.Year == year);
        }

        if (organizationUnitId is not null)
        {
            query = query.Where(r => r.Parameters.OrganizationUnitId == organizationUnitId);
        }

        if (employeeId is not null)
        {
            query = query.Where(r => r.EmployeeId == employeeId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                r.Employee.FirstName.ToLower().Contains(term) ||
                r.Employee.LastName.ToLower().Contains(term) ||
                (r.Employee.FirstName + " " + r.Employee.LastName).ToLower().Contains(term));
        }

        var latestResultIds = query
            .GroupBy(r => r.EmployeeId)
            .Select(g => g
                .OrderByDescending(r => r.CalculatedAt)
                .ThenByDescending(r => r.IsFinal)
                .ThenByDescending(r => r.Id)
                .Select(r => r.Id)
                .First());

        var totalCount = await latestResultIds.CountAsync(cancellationToken);
        var items = await this.context.VariableCompensationResults
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.OrganizationUnit)
            .Include(r => r.Parameters).ThenInclude(p => p.OrganizationUnit)
            .Where(r => latestResultIds.Contains(r.Id))
            .OrderByDescending(r => r.CalculatedAt)
            .ThenBy(r => r.Employee.LastName)
            .ThenBy(r => r.Employee.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<VariableCompensationResult>> GetResultsForAnalyticsAsync(
        short year,
        long? organizationUnitId,
        CancellationToken cancellationToken)
    {
        var query = this.context.VariableCompensationResults
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.OrganizationUnit)
            .Include(r => r.Parameters).ThenInclude(p => p.OrganizationUnit)
            .Where(r => r.Year == year);

        if (organizationUnitId is not null)
        {
            query = query.Where(r => r.Parameters.OrganizationUnitId == organizationUnitId);
        }

        return await query
            .OrderBy(r => r.Employee.LastName)
            .ThenBy(r => r.Employee.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetActiveEmployeesByOrganizationUnitAsync(
        long organizationUnitId,
        CancellationToken cancellationToken) =>
        await this.context.Employees
            .AsNoTracking()
            .Include(e => e.JobPosition)
            .Include(e => e.Evaluator)
            .Where(e => e.OrganizationUnitId == organizationUnitId && e.IsActive)
            .OrderBy(e => e.LastName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Evaluation>> GetApprovedEvaluationsAsync(
        long organizationUnitId,
        short year,
        CancellationToken cancellationToken) =>
        await this.context.Evaluations
            .AsNoTracking()
            .Include(e => e.Employee)
            .Where(e =>
                e.Employee.OrganizationUnitId == organizationUnitId &&
                e.Year == year &&
                e.Status == EvaluationStatus.Approved &&
                e.GoalsAverage != null &&
                e.MeasuresAverage != null &&
                e.OverallAverage != null)
            .OrderBy(e => e.EmployeeId)
            .ThenBy(e => e.Quarter)
            .ToListAsync(cancellationToken);

    public Task<EvaluatorSettings?> GetEvaluatorSettingsAsync(long evaluatorEmployeeId, CancellationToken cancellationToken) =>
        this.context.EvaluatorSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EmployeeId == evaluatorEmployeeId, cancellationToken);

    public Task<VariableCompensationResult?> FindResultForEmployeeAsync(
        long employeeId,
        long parametersId,
        short year,
        CancellationToken cancellationToken) =>
        this.context.VariableCompensationResults
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.EmployeeId == employeeId && r.ParametersId == parametersId && r.Year == year,
                cancellationToken);

    public Task<VariableCompensationResult?> FindResultForEmployeeForUpdateAsync(
        long employeeId,
        long parametersId,
        short year,
        CancellationToken cancellationToken) =>
        this.context.VariableCompensationResults
            .Include(r => r.EvaluationLinks)
            .FirstOrDefaultAsync(
                r => r.EmployeeId == employeeId && r.ParametersId == parametersId && r.Year == year,
                cancellationToken);

    public Task<bool> HasFinalResultsAsync(long parametersId, CancellationToken cancellationToken) =>
        this.context.VariableCompensationResults.AnyAsync(r => r.ParametersId == parametersId && r.IsFinal, cancellationToken);

    public async Task<CompensationCalculationStatus> GetCalculationStatusAsync(
        long parametersId,
        CancellationToken cancellationToken)
    {
        var rows = await this.context.VariableCompensationResults
            .AsNoTracking()
            .Where(r => r.ParametersId == parametersId)
            .Select(r => new { r.IsFinal, r.CalculatedAt })
            .ToListAsync(cancellationToken);

        var isFinalized = await this.HasFinalResultsAsync(parametersId, cancellationToken);

        return new CompensationCalculationStatus
        {
            ParametersId = parametersId,
            TotalResults = rows.Count,
            FinalizedResults = rows.Count(r => r.IsFinal),
            IsFinalized = isFinalized,
            LastCalculatedAt = rows.Count > 0 ? rows.Max(r => r.CalculatedAt) : null,
        };
    }

    public async Task<long?> ResolveActiveParametersIdAsync(
        long organizationUnitId,
        short year,
        CancellationToken cancellationToken)
    {
        var parameters = await this.GetParametersAsync(organizationUnitId, year, isActive: true, cancellationToken);
        return parameters.OrderByDescending(p => p.Id).FirstOrDefault()?.Id;
    }

    public async Task AddResultAsync(VariableCompensationResult entity, CancellationToken cancellationToken) =>
        await this.context.VariableCompensationResults.AddAsync(entity, cancellationToken);

    public Task RemoveResultAsync(VariableCompensationResult entity, CancellationToken cancellationToken)
    {
        this.context.VariableCompensationResults.Remove(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);
}
