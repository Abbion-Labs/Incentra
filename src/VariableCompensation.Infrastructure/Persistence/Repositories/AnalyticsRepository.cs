using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Infrastructure.Persistence.Repositories;

public sealed class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AppDbContext context;

    public AnalyticsRepository(AppDbContext context) => this.context = context;

    public Task<int> GetActiveSubordinateCountAsync(long evaluatorEmployeeId, CancellationToken cancellationToken) =>
        this.context.Employees
            .AsNoTracking()
            .CountAsync(
                e => e.IsActive && e.EvaluatorEmployeeId == evaluatorEmployeeId,
                cancellationToken);

    public async Task<IReadOnlyList<DescriptiveRatingCountRow>> GetDescriptiveRatingCountsByEvaluatorAsync(
        long evaluatorEmployeeId,
        short year,
        CancellationToken cancellationToken)
    {
        var ratings = await this.context.DescriptiveRatings
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(cancellationToken);

        var counts = await this.context.Evaluations
            .AsNoTracking()
            .Where(e =>
                e.EvaluatorEmployeeId == evaluatorEmployeeId &&
                e.Year == year &&
                e.DescriptiveRatingId != null &&
                e.Status != EvaluationStatus.Draft)
            .GroupBy(e => e.DescriptiveRatingId)
            .Select(g => new { DescriptiveRatingId = g.Key!.Value, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var countLookup = counts.ToDictionary(x => x.DescriptiveRatingId, x => x.Count);

        return ratings
            .Select(r => new DescriptiveRatingCountRow(
                r.Id,
                r.Code,
                r.Name,
                r.SortOrder,
                countLookup.GetValueOrDefault(r.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<decimal>> GetOverallAveragesByEvaluatorAsync(
        long evaluatorEmployeeId,
        short? year,
        CancellationToken cancellationToken)
    {
        var query = this.context.Evaluations
            .AsNoTracking()
            .Where(e =>
                e.EvaluatorEmployeeId == evaluatorEmployeeId &&
                e.OverallAverage != null &&
                e.DescriptiveRatingId != null &&
                e.Status != EvaluationStatus.Draft);

        if (year is not null)
        {
            query = query.Where(e => e.Year == year);
        }

        return await query
            .Select(e => e.OverallAverage!.Value)
            .ToListAsync(cancellationToken);
    }
}
