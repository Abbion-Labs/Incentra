using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common.Models;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Infrastructure.Persistence.Repositories;

public sealed class EvaluationRepository : IEvaluationRepository
{
    private readonly AppDbContext context;

    public EvaluationRepository(AppDbContext context) => this.context = context;

    public Task<Evaluation?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        IncludeDetails(this.context.Evaluations.AsNoTracking())
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Evaluation?> FindByIdForUpdateAsync(long id, CancellationToken cancellationToken) =>
        IncludeDetails(this.context.Evaluations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EvaluationStatusHistory>> GetStatusHistoryAsync(
        long evaluationId,
        CancellationToken cancellationToken) =>
        await this.context.EvaluationStatusHistories
            .AsNoTracking()
            .Where(h => h.EvaluationId == evaluationId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Evaluation> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        short? year,
        byte? quarter,
        EvaluationStatus? status,
        string? bucket,
        string? search,
        long? employeeId,
        long? evaluatorEmployeeId,
        long? controllerEmployeeId,
        long? organizationUnitId,
        CancellationToken cancellationToken)
    {
        var query = BuildFilteredQuery(year, quarter, status, bucket, search, employeeId, evaluatorEmployeeId, controllerEmployeeId, organizationUnitId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await IncludeListDetails(query)
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Quarter)
            .ThenBy(e => e.Employee.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<EvaluationBucketCountsResponse> GetBucketCountsAsync(
        short? year,
        byte? quarter,
        string? search,
        long? employeeId,
        long? evaluatorEmployeeId,
        long? controllerEmployeeId,
        long? organizationUnitId,
        CancellationToken cancellationToken)
    {
        var baseQuery = BuildFilteredQuery(year, quarter, status: null, bucket: null, search, employeeId, evaluatorEmployeeId, controllerEmployeeId, organizationUnitId);

        var planning = await EvaluationBucketFilter.ApplyBucket(baseQuery, "planning").CountAsync(cancellationToken);
        var unrated = await EvaluationBucketFilter.ApplyBucket(baseQuery, "unrated").CountAsync(cancellationToken);
        var returned = await EvaluationBucketFilter.ApplyBucket(baseQuery, "returned").CountAsync(cancellationToken);
        var submitted = await EvaluationBucketFilter.ApplyBucket(baseQuery, "submitted").CountAsync(cancellationToken);
        var approved = await EvaluationBucketFilter.ApplyBucket(baseQuery, "approved").CountAsync(cancellationToken);
        var goalsComplete = await EvaluationBucketFilter.ApplyBucket(baseQuery, "goalscomplete").CountAsync(cancellationToken);

        return new EvaluationBucketCountsResponse
        {
            Planning = planning,
            Unrated = unrated,
            Returned = returned,
            Submitted = submitted,
            Approved = approved,
            Pending = submitted,
            GoalsComplete = goalsComplete,
        };
    }

    private IQueryable<Evaluation> BuildFilteredQuery(
        short? year,
        byte? quarter,
        EvaluationStatus? status,
        string? bucket,
        string? search,
        long? employeeId,
        long? evaluatorEmployeeId,
        long? controllerEmployeeId,
        long? organizationUnitId)
    {
        var query = this.context.Evaluations
            .AsNoTracking()
            .Include(e => e.Employee)
            .Include(e => e.Evaluator)
            .AsQueryable();

        if (year is not null)
        {
            query = query.Where(e => e.Year == year);
        }

        if (quarter is not null)
        {
            query = query.Where(e => e.Quarter == quarter);
        }

        if (status is not null)
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(bucket) && EvaluationBucketFilter.TryParseBucket(bucket, out var normalizedBucket))
        {
            query = EvaluationBucketFilter.ApplyBucket(query, normalizedBucket);
        }

        query = EvaluationBucketFilter.ApplySearch(query, search);

        if (employeeId is not null)
        {
            query = query.Where(e => e.EmployeeId == employeeId);
        }

        if (evaluatorEmployeeId is not null)
        {
            query = query.Where(e => e.EvaluatorEmployeeId == evaluatorEmployeeId);
        }

        if (controllerEmployeeId is not null)
        {
            query = query.Where(e => e.ControllerEmployeeId == controllerEmployeeId);
        }

        if (organizationUnitId is not null)
        {
            query = query.Where(e => e.Employee.OrganizationUnitId == organizationUnitId);
        }

        return query;
    }

    private static IQueryable<Evaluation> IncludeListDetails(IQueryable<Evaluation> query) =>
        query
            .Include(e => e.Employee).ThenInclude(emp => emp.OrganizationUnit)
            .Include(e => e.Evaluator)
            .Include(e => e.Controller)
            .Include(e => e.DescriptiveRating)
            .Include(e => e.Goals).ThenInclude(g => g.RatingLevel)
            .Include(e => e.Measures).ThenInclude(m => m.RatingLevel)
            .Include(e => e.Conditions)
            .Include(e => e.Criteria);

    public Task<bool> ExistsForEmployeeQuarterAsync(
        long employeeId,
        short year,
        byte quarter,
        long? excludeId,
        CancellationToken cancellationToken) =>
        this.context.Evaluations.AnyAsync(
            e => e.EmployeeId == employeeId &&
                 e.Year == year &&
                 e.Quarter == quarter &&
                 (excludeId == null || e.Id != excludeId),
            cancellationToken);

    public async Task<long?> GetControllerEmployeeIdAsync(long evaluatorEmployeeId, CancellationToken cancellationToken)
    {
        var settings = await this.context.EvaluatorSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EmployeeId == evaluatorEmployeeId, cancellationToken);

        return settings?.ControllerEmployeeId;
    }

    public async Task<IReadOnlyList<Evaluation>> GetListByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken) =>
        await this.context.Evaluations
            .AsNoTracking()
            .Include(e => e.DescriptiveRating)
            .Include(e => e.Goals).ThenInclude(g => g.RatingLevel)
            .Include(e => e.Measures).ThenInclude(m => m.RatingLevel)
            .Include(e => e.Conditions)
            .Include(e => e.Criteria)
            .Where(e => e.EmployeeId == employeeId)
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Quarter)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ApprovedQuarterBenchmarkAverages>> GetApprovedBenchmarkAveragesByOrganizationUnitAsync(
        long organizationUnitId,
        CancellationToken cancellationToken) =>
        await this.context.Evaluations
            .AsNoTracking()
            .Where(e =>
                e.Status == EvaluationStatus.Approved &&
                !e.ExcludedFromCompensation &&
                e.Employee.OrganizationUnitId == organizationUnitId)
            .GroupBy(e => new { e.Year, e.Quarter })
            .Select(g => new ApprovedQuarterBenchmarkAverages(
                g.Key.Year,
                g.Key.Quarter,
                g.Average(e => e.OverallAverage),
                g.Average(e => e.GoalsAverage),
                g.Average(e => e.MeasuresAverage)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ApprovedQuarterBenchmarkAverages>> GetApprovedBenchmarkAveragesByJobPositionAsync(
        long jobPositionId,
        CancellationToken cancellationToken) =>
        await this.context.Evaluations
            .AsNoTracking()
            .Where(e =>
                e.Status == EvaluationStatus.Approved &&
                !e.ExcludedFromCompensation &&
                e.Employee.JobPositionId == jobPositionId)
            .GroupBy(e => new { e.Year, e.Quarter })
            .Select(g => new ApprovedQuarterBenchmarkAverages(
                g.Key.Year,
                g.Key.Quarter,
                g.Average(e => e.OverallAverage),
                g.Average(e => e.GoalsAverage),
                g.Average(e => e.MeasuresAverage)))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Evaluation entity, CancellationToken cancellationToken) =>
        await this.context.Evaluations.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);

    private static IQueryable<Evaluation> IncludeDetails(IQueryable<Evaluation> query) =>
        query
            .Include(e => e.Employee).ThenInclude(emp => emp.OrganizationUnit)
            .Include(e => e.Evaluator)
            .Include(e => e.Controller)
            .Include(e => e.DescriptiveRating)
            .Include(e => e.Goals).ThenInclude(g => g.RatingLevel)
            .Include(e => e.Measures).ThenInclude(m => m.MeasureType)
            .Include(e => e.Measures).ThenInclude(m => m.RatingLevel)
            .Include(e => e.Criteria)
            .Include(e => e.Conditions)
            .Include(e => e.Training);
}

public sealed class EvaluationLookupRepository : IEvaluationLookupRepository
{
    private readonly AppDbContext context;

    public EvaluationLookupRepository(AppDbContext context) => this.context = context;

    public async Task<IReadOnlyList<RatingLevel>> GetRatingLevelsAsync(CancellationToken cancellationToken) =>
        await this.context.RatingLevels
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Value)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DescriptiveRating>> GetDescriptiveRatingsAsync(CancellationToken cancellationToken) =>
        await this.context.DescriptiveRatings
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MeasureType>> GetMeasureTypesAsync(CancellationToken cancellationToken) =>
        await this.context.MeasureTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<bool> RatingLevelExistsAsync(long id, CancellationToken cancellationToken) =>
        this.context.RatingLevels.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);

    public Task<bool> MeasureTypeExistsAsync(long id, CancellationToken cancellationToken) =>
        this.context.MeasureTypes.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);
}
