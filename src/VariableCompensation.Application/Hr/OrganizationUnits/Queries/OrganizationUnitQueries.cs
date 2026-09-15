using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Hr.Models;

namespace VariableCompensation.Application.Hr.OrganizationUnits.Queries;

public sealed record GetOrganizationUnitsQuery(bool? IsActive) : IRequest<IReadOnlyList<OrganizationUnitResponse>>;

public sealed class GetOrganizationUnitsQueryHandler : IRequestHandler<GetOrganizationUnitsQuery, IReadOnlyList<OrganizationUnitResponse>>
{
    private readonly IOrganizationUnitRepository repository;

    public GetOrganizationUnitsQueryHandler(IOrganizationUnitRepository repository) => this.repository = repository;

    public async Task<IReadOnlyList<OrganizationUnitResponse>> Handle(GetOrganizationUnitsQuery request, CancellationToken cancellationToken)
    {
        var items = await this.repository.GetAllAsync(request.IsActive, cancellationToken);
        return items.Select(HrMappings.ToResponse).ToList();
    }
}

public sealed record GetOrganizationUnitByIdQuery(long Id) : IRequest<OrganizationUnitResponse?>;

public sealed class GetOrganizationUnitByIdQueryHandler : IRequestHandler<GetOrganizationUnitByIdQuery, OrganizationUnitResponse?>
{
    private readonly IOrganizationUnitRepository repository;

    public GetOrganizationUnitByIdQueryHandler(IOrganizationUnitRepository repository) => this.repository = repository;

    public async Task<OrganizationUnitResponse?> Handle(GetOrganizationUnitByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : HrMappings.ToResponse(entity);
    }
}
