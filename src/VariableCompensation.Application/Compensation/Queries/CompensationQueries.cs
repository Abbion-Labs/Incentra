using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common.Models;
using VariableCompensation.Application.Compensation.Models;
using VariableCompensation.Application.Compensation.Services;

namespace VariableCompensation.Application.Compensation.Queries;

public sealed record GetCompensationParametersQuery(
    long? OrganizationUnitId,
    short? Year,
    bool? IsActive) : IRequest<IReadOnlyList<CompensationParametersResponse>>;

public sealed class GetCompensationParametersQueryHandler : IRequestHandler<GetCompensationParametersQuery, IReadOnlyList<CompensationParametersResponse>>
{
    private readonly ICompensationRepository repository;

    public GetCompensationParametersQueryHandler(ICompensationRepository repository) => this.repository = repository;

    public async Task<IReadOnlyList<CompensationParametersResponse>> Handle(GetCompensationParametersQuery request, CancellationToken cancellationToken)
    {
        var items = await this.repository.GetParametersAsync(request.OrganizationUnitId, request.Year, request.IsActive, cancellationToken);
        return items.Select(CompensationMappings.ToResponse).ToList();
    }
}

public sealed record GetCompensationParametersByIdQuery(long Id) : IRequest<CompensationParametersResponse?>;

public sealed class GetCompensationParametersByIdQueryHandler : IRequestHandler<GetCompensationParametersByIdQuery, CompensationParametersResponse?>
{
    private readonly ICompensationRepository repository;

    public GetCompensationParametersByIdQueryHandler(ICompensationRepository repository) => this.repository = repository;

    public async Task<CompensationParametersResponse?> Handle(GetCompensationParametersByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindParametersByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : CompensationMappings.ToResponse(entity);
    }
}

public sealed record GetCompensationResultsQuery(
    int Page,
    int PageSize,
    long? ParametersId,
    short? Year,
    long? OrganizationUnitId,
    long? EmployeeId,
    string? Search) : IRequest<PagedResult<CompensationResultSummaryResponse>>;

public sealed class GetCompensationResultsQueryHandler : IRequestHandler<GetCompensationResultsQuery, PagedResult<CompensationResultSummaryResponse>>
{
    private readonly ICompensationRepository repository;
    private readonly CompensationAccessService accessService;

    public GetCompensationResultsQueryHandler(ICompensationRepository repository, CompensationAccessService accessService)
    {
        this.repository = repository;
        this.accessService = accessService;
    }

    public async Task<PagedResult<CompensationResultSummaryResponse>> Handle(GetCompensationResultsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (employeeId, organizationUnitId) = await this.accessService.ResolveResultListFiltersAsync(
            request.EmployeeId,
            request.OrganizationUnitId,
            cancellationToken);

        if (employeeId == -1)
        {
            return new PagedResult<CompensationResultSummaryResponse> { Page = page, PageSize = pageSize };
        }

        var parametersId = request.ParametersId;
        if (parametersId is null && organizationUnitId is not null && request.Year is not null)
        {
            parametersId = await this.repository.ResolveActiveParametersIdAsync(
                organizationUnitId.Value,
                request.Year.Value,
                cancellationToken);
        }

        var (items, totalCount) = await this.repository.GetResultsPagedAsync(
            page,
            pageSize,
            parametersId,
            request.Year,
            organizationUnitId,
            employeeId,
            request.Search,
            cancellationToken);

        return new PagedResult<CompensationResultSummaryResponse>
        {
            Items = items.Select(CompensationMappings.ToSummary).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

public sealed record GetCompensationResultByIdQuery(long Id) : IRequest<CompensationResultDetailResponse?>;

public sealed class GetCompensationResultByIdQueryHandler : IRequestHandler<GetCompensationResultByIdQuery, CompensationResultDetailResponse?>
{
    private readonly ICompensationRepository repository;
    private readonly CompensationAccessService accessService;

    public GetCompensationResultByIdQueryHandler(
        ICompensationRepository repository,
        CompensationAccessService accessService)
    {
        this.repository = repository;
        this.accessService = accessService;
    }

    public async Task<CompensationResultDetailResponse?> Handle(GetCompensationResultByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindResultByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!await this.accessService.CanViewResultAsync(entity, cancellationToken))
        {
            return null;
        }

        return CompensationMappings.ToDetail(entity);
    }
}

public sealed record GetCompensationCalculationStatusQuery(long ParametersId)
    : IRequest<CompensationCalculationStatus?>;

public sealed class GetCompensationCalculationStatusQueryHandler
    : IRequestHandler<GetCompensationCalculationStatusQuery, CompensationCalculationStatus?>
{
    private readonly ICompensationRepository repository;

    public GetCompensationCalculationStatusQueryHandler(ICompensationRepository repository) =>
        this.repository = repository;

    public async Task<CompensationCalculationStatus?> Handle(
        GetCompensationCalculationStatusQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = await this.repository.FindParametersByIdAsync(request.ParametersId, cancellationToken);
        if (parameters is null)
        {
            return null;
        }

        return await this.repository.GetCalculationStatusAsync(request.ParametersId, cancellationToken);
    }
}
