using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Hr.EvaluatorSettings.Queries;

public sealed record GetEvaluatorSettingsListQuery : IRequest<IReadOnlyList<EvaluatorSettingsResponse>>;

public sealed class GetEvaluatorSettingsListQueryHandler : IRequestHandler<GetEvaluatorSettingsListQuery, IReadOnlyList<EvaluatorSettingsResponse>>
{
    private readonly IEvaluatorSettingsRepository repository;

    public GetEvaluatorSettingsListQueryHandler(IEvaluatorSettingsRepository repository) => this.repository = repository;

    public async Task<IReadOnlyList<EvaluatorSettingsResponse>> Handle(GetEvaluatorSettingsListQuery request, CancellationToken cancellationToken)
    {
        var items = await this.repository.GetAllAsync(cancellationToken);
        return items.Select(HrMappings.ToResponse).ToList();
    }
}

public sealed record GetEvaluatorsForControllerQuery : IRequest<IReadOnlyList<ControllerEvaluatorSummaryResponse>>;

public sealed class GetEvaluatorsForControllerQueryHandler
    : IRequestHandler<GetEvaluatorsForControllerQuery, IReadOnlyList<ControllerEvaluatorSummaryResponse>>
{
    private readonly IEvaluatorSettingsRepository repository;
    private readonly IAnalyticsRepository analyticsRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;

    public GetEvaluatorsForControllerQueryHandler(
        IEvaluatorSettingsRepository repository,
        IAnalyticsRepository analyticsRepository,
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext)
    {
        this.repository = repository;
        this.analyticsRepository = analyticsRepository;
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
    }

    public async Task<IReadOnlyList<ControllerEvaluatorSummaryResponse>> Handle(
        GetEvaluatorsForControllerQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.Hr.EvaluatorSettings> items;

        if (this.currentUserService.IsAdmin)
        {
            items = await this.repository.GetAllAsync(cancellationToken);
        }
        else if (!this.currentUserService.IsInRole(RoleCodes.Controller))
        {
            return [];
        }
        else
        {
            var controllerEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
            if (controllerEmployeeId is null)
            {
                return [];
            }

            items = await this.repository.GetByControllerEmployeeIdAsync(controllerEmployeeId.Value, cancellationToken);
        }

        var results = new List<ControllerEvaluatorSummaryResponse>(items.Count);
        foreach (var item in items)
        {
            var subordinateCount = await this.analyticsRepository.GetActiveSubordinateCountAsync(
                item.EmployeeId,
                cancellationToken);
            results.Add(HrMappings.ToControllerEvaluatorSummary(item, subordinateCount));
        }

        return results;
    }
}

public sealed record GetEvaluatorSettingsByEmployeeIdQuery(long EmployeeId) : IRequest<EvaluatorSettingsResponse?>;

public sealed class GetEvaluatorSettingsByEmployeeIdQueryHandler : IRequestHandler<GetEvaluatorSettingsByEmployeeIdQuery, EvaluatorSettingsResponse?>
{
    private readonly IEvaluatorSettingsRepository repository;
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;

    public GetEvaluatorSettingsByEmployeeIdQueryHandler(
        IEvaluatorSettingsRepository repository,
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext)
    {
        this.repository = repository;
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
    }

    public async Task<EvaluatorSettingsResponse?> Handle(GetEvaluatorSettingsByEmployeeIdQuery request, CancellationToken cancellationToken)
    {
        if (!this.currentUserService.IsAdmin)
        {
            var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
            if (currentEmployeeId != request.EmployeeId)
            {
                return null;
            }
        }

        var entity = await this.repository.FindByEmployeeIdAsync(request.EmployeeId, cancellationToken);
        return entity is null ? null : HrMappings.ToResponse(entity);
    }
}

public sealed record GetMyEvaluatorSettingsQuery : IRequest<EvaluatorSettingsResponse?>;

public sealed class GetMyEvaluatorSettingsQueryHandler : IRequestHandler<GetMyEvaluatorSettingsQuery, EvaluatorSettingsResponse?>
{
    private readonly IEvaluatorSettingsRepository repository;
    private readonly ICurrentEmployeeContext currentEmployeeContext;

    public GetMyEvaluatorSettingsQueryHandler(
        IEvaluatorSettingsRepository repository,
        ICurrentEmployeeContext currentEmployeeContext)
    {
        this.repository = repository;
        this.currentEmployeeContext = currentEmployeeContext;
    }

    public async Task<EvaluatorSettingsResponse?> Handle(GetMyEvaluatorSettingsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (employeeId is null)
        {
            return null;
        }

        var entity = await this.repository.FindByEmployeeIdAsync(employeeId.Value, cancellationToken);
        return entity is null ? null : HrMappings.ToResponse(entity);
    }
}
