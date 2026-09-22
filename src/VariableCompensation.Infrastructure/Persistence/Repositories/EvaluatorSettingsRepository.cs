using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Infrastructure.Persistence.Repositories;

public sealed class EvaluatorSettingsRepository : IEvaluatorSettingsRepository
{
    private readonly AppDbContext context;

    public EvaluatorSettingsRepository(AppDbContext context) => this.context = context;

    public Task<EvaluatorSettings?> FindByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken) =>
        this.BuildQuery(asNoTracking: true)
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, cancellationToken);

    public Task<EvaluatorSettings?> FindByEmployeeIdForUpdateAsync(long employeeId, CancellationToken cancellationToken) =>
        this.BuildQuery(asNoTracking: false)
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, cancellationToken);

    public async Task<IReadOnlyList<EvaluatorSettings>> GetAllAsync(CancellationToken cancellationToken) =>
        await this.BuildQuery(asNoTracking: true)
            .OrderBy(s => s.Employee.LastName)
            .ThenBy(s => s.Employee.FirstName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EvaluatorSettings>> GetByControllerEmployeeIdAsync(
        long controllerEmployeeId,
        CancellationToken cancellationToken) =>
        await this.BuildQuery(asNoTracking: true)
            .Where(s => s.ControllerEmployeeId == controllerEmployeeId)
            .OrderBy(s => s.Employee.LastName)
            .ThenBy(s => s.Employee.FirstName)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(long employeeId, CancellationToken cancellationToken) =>
        this.context.EvaluatorSettings.AnyAsync(s => s.EmployeeId == employeeId, cancellationToken);

    public async Task AddAsync(EvaluatorSettings entity, CancellationToken cancellationToken) =>
        await this.context.EvaluatorSettings.AddAsync(entity, cancellationToken);

    public Task<bool> IsControllerForAnyEvaluatorAsync(long controllerEmployeeId, CancellationToken cancellationToken) =>
        this.context.EvaluatorSettings.AnyAsync(s => s.ControllerEmployeeId == controllerEmployeeId, cancellationToken);

    public void Remove(EvaluatorSettings entity) => this.context.EvaluatorSettings.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);

    private IQueryable<EvaluatorSettings> BuildQuery(bool asNoTracking)
    {
        var query = this.context.EvaluatorSettings
            .Include(s => s.Employee)
                .ThenInclude(e => e.OrganizationUnit)
            .Include(s => s.Employee)
                .ThenInclude(e => e.JobPosition)
            .Include(s => s.Controller)
            .AsQueryable();

        return asNoTracking ? query.AsNoTracking() : query;
    }
}
