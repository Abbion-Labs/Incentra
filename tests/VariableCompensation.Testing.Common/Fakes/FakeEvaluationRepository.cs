using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common.Models;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Enums;
using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeEvaluationRepository : IEvaluationRepository
{
    private readonly Dictionary<long, EvaluationEntity> store = new();
    private long nextId = 1;

    public IReadOnlyDictionary<long, EvaluationEntity> Store => this.store;

    public void Seed(params EvaluationEntity[] evaluations)
    {
        foreach (var evaluation in evaluations)
        {
            if (evaluation.Id == 0)
            {
                evaluation.Id = this.nextId++;
            }
            else
            {
                this.nextId = Math.Max(this.nextId, evaluation.Id + 1);
            }

            EnsureNavigationProperties(evaluation);
            this.store[evaluation.Id] = Clone(evaluation);
        }
    }

    private static void EnsureNavigationProperties(EvaluationEntity evaluation)
    {
        evaluation.Employee ??= new Domain.Entities.Hr.Employee
        {
            Id = evaluation.EmployeeId,
            FirstName = "Marko",
            LastName = "Marković",
            OrganizationUnit = new Domain.Entities.Lookup.OrganizationUnit { Name = "IT" },
        };
        evaluation.Evaluator ??= new Domain.Entities.Hr.Employee
        {
            Id = evaluation.EvaluatorEmployeeId,
            FirstName = "Jovan",
            LastName = "Ocenjivač",
        };
        if (evaluation.ControllerEmployeeId is not null)
        {
            evaluation.Controller ??= new Domain.Entities.Hr.Employee
            {
                Id = evaluation.ControllerEmployeeId.Value,
                FirstName = "Milan",
                LastName = "Kontroler",
            };
        }
    }

    public Task<EvaluationEntity?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(this.store.TryGetValue(id, out var entity) ? Clone(entity) : null);

    public Task<EvaluationEntity?> FindByIdForUpdateAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(this.store.TryGetValue(id, out var entity) ? entity : null);

    public Task<IReadOnlyList<EvaluationStatusHistory>> GetStatusHistoryAsync(long evaluationId, CancellationToken cancellationToken)
    {
        if (!this.store.TryGetValue(evaluationId, out var entity))
        {
            return Task.FromResult<IReadOnlyList<EvaluationStatusHistory>>([]);
        }

        return Task.FromResult<IReadOnlyList<EvaluationStatusHistory>>(entity.StatusHistory.ToList());
    }

    public Task<(IReadOnlyList<EvaluationEntity> Items, int TotalCount)> GetPagedAsync(
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
        var items = this.store.Values.AsEnumerable();
        if (employeeId is not null)
        {
            items = items.Where(e => e.EmployeeId == employeeId);
        }

        if (evaluatorEmployeeId is not null)
        {
            items = items.Where(e => e.EvaluatorEmployeeId == evaluatorEmployeeId);
        }

        if (controllerEmployeeId is not null)
        {
            items = items.Where(e => e.ControllerEmployeeId == controllerEmployeeId);
        }

        var list = items.ToList();
        return Task.FromResult<(IReadOnlyList<EvaluationEntity>, int)>((list, list.Count));
    }

    public Task<EvaluationBucketCountsResponse> GetBucketCountsAsync(
        short? year,
        byte? quarter,
        string? search,
        long? employeeId,
        long? evaluatorEmployeeId,
        long? controllerEmployeeId,
        long? organizationUnitId,
        CancellationToken cancellationToken) =>
        Task.FromResult(new EvaluationBucketCountsResponse());

    public Task<bool> ExistsForEmployeeQuarterAsync(long employeeId, short year, byte quarter, long? excludeId, CancellationToken cancellationToken) =>
        Task.FromResult(this.store.Values.Any(e =>
            e.EmployeeId == employeeId &&
            e.Year == year &&
            e.Quarter == quarter &&
            (excludeId is null || e.Id != excludeId)));

    public Task<long?> GetControllerEmployeeIdAsync(long evaluatorEmployeeId, CancellationToken cancellationToken) =>
        Task.FromResult<long?>(3);

    public Task<IReadOnlyList<EvaluationEntity>> GetListByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EvaluationEntity>>(this.store.Values.Where(e => e.EmployeeId == employeeId).Select(Clone).ToList());

    public Task<IReadOnlyList<ApprovedQuarterBenchmarkAverages>> GetApprovedBenchmarkAveragesByOrganizationUnitAsync(
        long organizationUnitId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ApprovedQuarterBenchmarkAverages>>([]);

    public Task<IReadOnlyList<ApprovedQuarterBenchmarkAverages>> GetApprovedBenchmarkAveragesByJobPositionAsync(
        long jobPositionId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ApprovedQuarterBenchmarkAverages>>([]);

    public Task<bool> HasUnapprovedForEmployeeAsync(long employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(this.store.Values.Any(e => e.EmployeeId == employeeId && e.Status != EvaluationStatus.Approved));

    public Task<IReadOnlyList<EvaluationEntity>> GetForUpdateByEmployeeAsync(
        long employeeId,
        EvaluationStatus status,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EvaluationEntity>>(
            this.store.Values.Where(e => e.EmployeeId == employeeId && e.Status == status).ToList());

    public Task<IReadOnlyList<EvaluationEntity>> GetUnapprovedForUpdateByEvaluatorAsync(
        long evaluatorEmployeeId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EvaluationEntity>>(
            this.store.Values
                .Where(e => e.EvaluatorEmployeeId == evaluatorEmployeeId && e.Status != EvaluationStatus.Approved)
                .ToList());

    public Task AddAsync(EvaluationEntity entity, CancellationToken cancellationToken)
    {
        entity.Id = this.nextId++;
        this.store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static EvaluationEntity Clone(EvaluationEntity source) => new()
    {
        Id = source.Id,
        EmployeeId = source.EmployeeId,
        EvaluatorEmployeeId = source.EvaluatorEmployeeId,
        ControllerEmployeeId = source.ControllerEmployeeId,
        Year = source.Year,
        Quarter = source.Quarter,
        Status = source.Status,
        Version = source.Version,
        GoalsAverage = source.GoalsAverage,
        MeasuresAverage = source.MeasuresAverage,
        OverallAverage = source.OverallAverage,
        DescriptiveRatingId = source.DescriptiveRatingId,
        SubmittedAt = source.SubmittedAt,
        ReviewedAt = source.ReviewedAt,
        ApprovedAt = source.ApprovedAt,
        ControllerComment = source.ControllerComment,
        RejectionReason = source.RejectionReason,
        EvaluatorComment = source.EvaluatorComment,
        ConditionsNotMetComment = source.ConditionsNotMetComment,
        ConditionsFulfilled = source.ConditionsFulfilled,
        ExcludedFromCompensation = source.ExcludedFromCompensation,
        Goals = source.Goals.Select(g => new EvaluationGoal
        {
            Id = g.Id,
            EvaluationId = g.EvaluationId,
            Description = g.Description,
            RatingLevelId = g.RatingLevelId,
            Comment = g.Comment,
            Weight = g.Weight,
            SortOrder = g.SortOrder,
        }).ToList(),
        Conditions = source.Conditions.Select(c => new EvaluationCondition
        {
            Id = c.Id,
            EvaluationId = c.EvaluationId,
            Description = c.Description,
            SortOrder = c.SortOrder,
        }).ToList(),
        Criteria = source.Criteria.Select(c => new EvaluationCriterion
        {
            Id = c.Id,
            EvaluationId = c.EvaluationId,
            Description = c.Description,
            SortOrder = c.SortOrder,
        }).ToList(),
        Measures = source.Measures.Select(m => new EvaluationMeasure
        {
            Id = m.Id,
            EvaluationId = m.EvaluationId,
            MeasureTypeId = m.MeasureTypeId,
            RatingLevelId = m.RatingLevelId,
            SortOrder = m.SortOrder,
        }).ToList(),
        StatusHistory = source.StatusHistory.Select(h => new EvaluationStatusHistory
        {
            Id = h.Id,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ChangedByUserId = h.ChangedByUserId,
            Comment = h.Comment,
            ChangedAt = h.ChangedAt,
        }).ToList(),
        Employee = new Domain.Entities.Hr.Employee
        {
            Id = source.EmployeeId,
            FirstName = source.Employee?.FirstName ?? "Marko",
            LastName = source.Employee?.LastName ?? "Marković",
            OrganizationUnit = source.Employee?.OrganizationUnit ?? new OrganizationUnit { Name = "IT" },
        },
        Evaluator = new Domain.Entities.Hr.Employee
        {
            Id = source.EvaluatorEmployeeId,
            FirstName = source.Evaluator?.FirstName ?? "Jovan",
            LastName = source.Evaluator?.LastName ?? "Ocenjivač",
        },
        Controller = source.ControllerEmployeeId is null
            ? null
            : new Domain.Entities.Hr.Employee
            {
                Id = source.ControllerEmployeeId.Value,
                FirstName = source.Controller?.FirstName ?? "Milan",
                LastName = source.Controller?.LastName ?? "Kontroler",
            },
    };
}
