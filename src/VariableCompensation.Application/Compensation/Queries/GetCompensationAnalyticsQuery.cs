using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Compensation.Models;
using VariableCompensation.Application.Compensation.Services;

namespace VariableCompensation.Application.Compensation.Queries;

public sealed record GetCompensationAnalyticsQuery(
    short Year,
    long? OrganizationUnitId,
    string ChartType) : IRequest<CompensationAnalyticsResponse?>;

public sealed class GetCompensationAnalyticsQueryHandler : IRequestHandler<GetCompensationAnalyticsQuery, CompensationAnalyticsResponse?>
{
    private readonly ICompensationRepository repository;
    private readonly CompensationAccessService accessService;
    private readonly CompensationAnalyticsService analyticsService;

    public GetCompensationAnalyticsQueryHandler(
        ICompensationRepository repository,
        CompensationAccessService accessService,
        CompensationAnalyticsService analyticsService)
    {
        this.repository = repository;
        this.accessService = accessService;
        this.analyticsService = analyticsService;
    }

    public async Task<CompensationAnalyticsResponse?> Handle(GetCompensationAnalyticsQuery request, CancellationToken cancellationToken)
    {
        if (!CompensationAnalyticsChartTypes.All.Contains(request.ChartType))
        {
            return null;
        }

        var (_, organizationUnitId) = await this.accessService.ResolveResultListFiltersAsync(
            null,
            request.OrganizationUnitId,
            cancellationToken);

        if (organizationUnitId == -1)
        {
            return null;
        }

        var results = await this.repository.GetResultsForAnalyticsAsync(
            request.Year,
            organizationUnitId,
            cancellationToken);

        var latestResults = CompensationResultSelection.LatestPerEmployee(results);

        var parameters = await this.repository.GetParametersAsync(
            organizationUnitId,
            request.Year,
            isActive: null,
            cancellationToken);

        var currency = parameters.FirstOrDefault()?.Currency ?? "RSD";
        var firstParameter = parameters.FirstOrDefault();
        string? organizationUnitName = organizationUnitId is null
            ? null
            : firstParameter?.OrganizationUnit?.Name
              ?? latestResults.FirstOrDefault()?.Parameters.OrganizationUnit?.Name
              ?? latestResults.FirstOrDefault()?.Employee.OrganizationUnit?.Name;
        var organizationUnitKey = organizationUnitId is null ? "all" : null;

        var response = this.analyticsService.Build(
            request.ChartType,
            request.Year,
            currency,
            organizationUnitName,
            latestResults);
        response.OrganizationUnitKey = organizationUnitKey;
        return response;
    }
}
