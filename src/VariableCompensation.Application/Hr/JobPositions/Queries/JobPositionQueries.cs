using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Hr.Models;

namespace VariableCompensation.Application.Hr.JobPositions.Queries;

public sealed record GetJobPositionsQuery(bool? IsActive) : IRequest<IReadOnlyList<JobPositionResponse>>;

public sealed class GetJobPositionsQueryHandler : IRequestHandler<GetJobPositionsQuery, IReadOnlyList<JobPositionResponse>>
{
    private readonly IJobPositionRepository repository;

    public GetJobPositionsQueryHandler(IJobPositionRepository repository) => this.repository = repository;

    public async Task<IReadOnlyList<JobPositionResponse>> Handle(GetJobPositionsQuery request, CancellationToken cancellationToken)
    {
        var items = await this.repository.GetAllAsync(request.IsActive, cancellationToken);
        return items.Select(HrMappings.ToResponse).ToList();
    }
}
