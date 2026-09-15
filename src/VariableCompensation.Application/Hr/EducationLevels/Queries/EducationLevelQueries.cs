using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Hr.Models;

namespace VariableCompensation.Application.Hr.EducationLevels.Queries;

public sealed record GetEducationLevelsQuery(bool? IsActive) : IRequest<IReadOnlyList<EducationLevelResponse>>;

public sealed class GetEducationLevelsQueryHandler : IRequestHandler<GetEducationLevelsQuery, IReadOnlyList<EducationLevelResponse>>
{
    private readonly IEducationLevelRepository repository;

    public GetEducationLevelsQueryHandler(IEducationLevelRepository repository) => this.repository = repository;

    public async Task<IReadOnlyList<EducationLevelResponse>> Handle(GetEducationLevelsQuery request, CancellationToken cancellationToken)
    {
        var items = await this.repository.GetAllAsync(request.IsActive, cancellationToken);
        return items.Select(HrMappings.ToResponse).ToList();
    }
}
