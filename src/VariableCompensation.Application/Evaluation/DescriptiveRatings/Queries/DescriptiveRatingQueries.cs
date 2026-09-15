using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Evaluation.Models;
using static VariableCompensation.Application.Evaluation.EvaluationMappings;

namespace VariableCompensation.Application.Evaluation.DescriptiveRatings.Queries;

public sealed record GetDescriptiveRatingsAdminQuery(bool? IsActive = null)
    : IRequest<IReadOnlyList<DescriptiveRatingResponse>>;

public sealed class GetDescriptiveRatingsAdminQueryHandler
    : IRequestHandler<GetDescriptiveRatingsAdminQuery, IReadOnlyList<DescriptiveRatingResponse>>
{
    private readonly IDescriptiveRatingRepository repository;

    public GetDescriptiveRatingsAdminQueryHandler(IDescriptiveRatingRepository repository) =>
        this.repository = repository;

    public async Task<IReadOnlyList<DescriptiveRatingResponse>> Handle(
        GetDescriptiveRatingsAdminQuery request,
        CancellationToken cancellationToken)
    {
        var items = await this.repository.GetAllAsync(request.IsActive, cancellationToken);
        return items.Select(ToDescriptiveRating).ToList();
    }
}
