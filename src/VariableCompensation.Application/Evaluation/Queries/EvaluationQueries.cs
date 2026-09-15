using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common.Models;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Evaluation.Queries;

public sealed record GetEvaluationsQuery(
    int Page,
    int PageSize,
    short? Year,
    byte? Quarter,
    EvaluationStatus? Status,
    string? Bucket,
    string? Search,
    long? EmployeeId,
    long? EvaluatorEmployeeId,
    long? ControllerEmployeeId,
    long? OrganizationUnitId) : IRequest<PagedResult<EvaluationSummaryResponse>>;

public sealed class GetEvaluationsQueryHandler : IRequestHandler<GetEvaluationsQuery, PagedResult<EvaluationSummaryResponse>>
{
    private readonly IEvaluationRepository repository;
    private readonly EvaluationAccessService accessService;

    public GetEvaluationsQueryHandler(IEvaluationRepository repository, EvaluationAccessService accessService)
    {
        this.repository = repository;
        this.accessService = accessService;
    }

    public async Task<PagedResult<EvaluationSummaryResponse>> Handle(GetEvaluationsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (employeeId, evaluatorEmployeeId, controllerEmployeeId) = await this.accessService.ResolveListFiltersAsync(
            request.EmployeeId,
            request.EvaluatorEmployeeId,
            request.ControllerEmployeeId,
            cancellationToken);

        if (employeeId == -1)
        {
            return new PagedResult<EvaluationSummaryResponse>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        var (items, totalCount) = await this.repository.GetPagedAsync(
            page,
            pageSize,
            request.Year,
            request.Quarter,
            request.Status,
            request.Bucket,
            request.Search,
            employeeId,
            evaluatorEmployeeId,
            controllerEmployeeId,
            request.OrganizationUnitId,
            cancellationToken);

        return new PagedResult<EvaluationSummaryResponse>
        {
            Items = items.Select(EvaluationMappings.ToSummary).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

public sealed record GetEvaluationBucketCountsQuery(
    short? Year,
    byte? Quarter,
    string? Search,
    long? EmployeeId,
    long? EvaluatorEmployeeId,
    long? ControllerEmployeeId,
    long? OrganizationUnitId) : IRequest<EvaluationBucketCountsResponse>;

public sealed class GetEvaluationBucketCountsQueryHandler : IRequestHandler<GetEvaluationBucketCountsQuery, EvaluationBucketCountsResponse>
{
    private readonly IEvaluationRepository repository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly EvaluationAccessService accessService;

    public GetEvaluationBucketCountsQueryHandler(
        IEvaluationRepository repository,
        IEmployeeRepository employeeRepository,
        EvaluationAccessService accessService)
    {
        this.repository = repository;
        this.employeeRepository = employeeRepository;
        this.accessService = accessService;
    }

    public async Task<EvaluationBucketCountsResponse> Handle(GetEvaluationBucketCountsQuery request, CancellationToken cancellationToken)
    {
        var (employeeId, evaluatorEmployeeId, controllerEmployeeId) = await this.accessService.ResolveListFiltersAsync(
            request.EmployeeId,
            request.EvaluatorEmployeeId,
            request.ControllerEmployeeId,
            cancellationToken);

        if (employeeId == -1)
        {
            return new EvaluationBucketCountsResponse();
        }

        var counts = await this.repository.GetBucketCountsAsync(
            request.Year,
            request.Quarter,
            request.Search,
            employeeId,
            evaluatorEmployeeId,
            controllerEmployeeId,
            request.OrganizationUnitId,
            cancellationToken);

        var goalsPending = 0;
        if (request.Year is not null && request.Quarter is not null)
        {
            var (_, pendingCount) = await this.employeeRepository.GetPagedAsync(
                1,
                1,
                request.OrganizationUnitId,
                evaluatorEmployeeId,
                request.Search,
                true,
                controllerEmployeeId,
                request.Year,
                request.Quarter,
                "pending",
                cancellationToken);
            goalsPending = pendingCount;
        }

        return new EvaluationBucketCountsResponse
        {
            Planning = counts.Planning,
            Unrated = counts.Unrated,
            Returned = counts.Returned,
            Submitted = counts.Submitted,
            Approved = counts.Approved,
            Pending = counts.Pending,
            GoalsComplete = counts.GoalsComplete,
            GoalsPending = goalsPending,
        };
    }
}

public sealed record GetEvaluationByIdQuery(long Id) : IRequest<EvaluationDetailResponse?>;

public sealed class GetEvaluationByIdQueryHandler : IRequestHandler<GetEvaluationByIdQuery, EvaluationDetailResponse?>
{
    private readonly IEvaluationRepository repository;
    private readonly EvaluationAccessService accessService;

    public GetEvaluationByIdQueryHandler(IEvaluationRepository repository, EvaluationAccessService accessService)
    {
        this.repository = repository;
        this.accessService = accessService;
    }

    public async Task<EvaluationDetailResponse?> Handle(GetEvaluationByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var access = await this.accessService.EnsureCanViewAsync(entity, cancellationToken);
        if (access.IsFailure)
        {
            return null;
        }

        var reviewAccess = await this.accessService.EnsureCanReviewAsync(entity, cancellationToken);
        if (reviewAccess.IsSuccess &&
            entity.ControllerViewedAt is null &&
            (entity.Status == EvaluationStatus.Submitted || entity.Status == EvaluationStatus.UnderReview))
        {
            var forUpdate = await this.repository.FindByIdForUpdateAsync(request.Id, cancellationToken);
            if (forUpdate is not null)
            {
                forUpdate.ControllerViewedAt = DateTime.UtcNow;
                await this.repository.SaveChangesAsync(cancellationToken);
                entity = forUpdate;
            }
        }

        return EvaluationMappings.ToDetail(entity);
    }
}

public sealed record GetEvaluationStatusHistoryQuery(long Id) : IRequest<IReadOnlyList<EvaluationStatusHistoryResponse>>;

public sealed class GetEvaluationStatusHistoryQueryHandler : IRequestHandler<GetEvaluationStatusHistoryQuery, IReadOnlyList<EvaluationStatusHistoryResponse>>
{
    private readonly IEvaluationRepository repository;
    private readonly EvaluationAccessService accessService;

    public GetEvaluationStatusHistoryQueryHandler(IEvaluationRepository repository, EvaluationAccessService accessService)
    {
        this.repository = repository;
        this.accessService = accessService;
    }

    public async Task<IReadOnlyList<EvaluationStatusHistoryResponse>> Handle(
        GetEvaluationStatusHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Array.Empty<EvaluationStatusHistoryResponse>();
        }

        var access = await this.accessService.EnsureCanViewAsync(entity, cancellationToken);
        if (access.IsFailure)
        {
            return Array.Empty<EvaluationStatusHistoryResponse>();
        }

        var items = await this.repository.GetStatusHistoryAsync(request.Id, cancellationToken);
        return items.Select(EvaluationMappings.ToStatusHistory).ToList();
    }
}

public sealed record GetRatingLevelsQuery : IRequest<IReadOnlyList<RatingLevelResponse>>;

public sealed class GetRatingLevelsQueryHandler : IRequestHandler<GetRatingLevelsQuery, IReadOnlyList<RatingLevelResponse>>
{
    private readonly IEvaluationLookupRepository repository;

    public GetRatingLevelsQueryHandler(IEvaluationLookupRepository repository) => this.repository = repository;

    public async Task<IReadOnlyList<RatingLevelResponse>> Handle(GetRatingLevelsQuery request, CancellationToken cancellationToken)
    {
        var items = await this.repository.GetRatingLevelsAsync(cancellationToken);
        return items.Select(EvaluationMappings.ToRatingLevel).ToList();
    }
}

public sealed record GetDescriptiveRatingsQuery : IRequest<IReadOnlyList<DescriptiveRatingResponse>>;

public sealed class GetDescriptiveRatingsQueryHandler : IRequestHandler<GetDescriptiveRatingsQuery, IReadOnlyList<DescriptiveRatingResponse>>
{
    private readonly IEvaluationLookupRepository repository;

    public GetDescriptiveRatingsQueryHandler(IEvaluationLookupRepository repository) => this.repository = repository;

    public async Task<IReadOnlyList<DescriptiveRatingResponse>> Handle(GetDescriptiveRatingsQuery request, CancellationToken cancellationToken)
    {
        var items = await this.repository.GetDescriptiveRatingsAsync(cancellationToken);
        return items.Select(EvaluationMappings.ToDescriptiveRating).ToList();
    }
}

public sealed record GetMeasureTypesQuery(bool IncludeDescriptions) : IRequest<IReadOnlyList<MeasureTypeResponse>>;

public sealed class GetMeasureTypesQueryHandler : IRequestHandler<GetMeasureTypesQuery, IReadOnlyList<MeasureTypeResponse>>
{
    private readonly IEvaluationLookupRepository repository;

    public GetMeasureTypesQueryHandler(IEvaluationLookupRepository repository) => this.repository = repository;

    public async Task<IReadOnlyList<MeasureTypeResponse>> Handle(GetMeasureTypesQuery request, CancellationToken cancellationToken)
    {
        var items = await this.repository.GetMeasureTypesAsync(request.IncludeDescriptions, cancellationToken);
        return items.Select(m => EvaluationMappings.ToMeasureType(m, request.IncludeDescriptions)).ToList();
    }
}
