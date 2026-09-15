using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Evaluation;
using VariableCompensation.Application.Evaluation.DescriptiveRatings.Commands;
using VariableCompensation.Application.Evaluation.DescriptiveRatings.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/descriptive-ratings")]
public sealed class DescriptiveRatingsController : ControllerBase
{
    private readonly IMediator mediator;

    public DescriptiveRatingsController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetDescriptiveRatingsAdminQuery(isActive), cancellationToken);
        return this.Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDescriptiveRatingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new CreateDescriptiveRatingCommand(
                request.Code,
                request.Name,
                request.MinAverage,
                request.MaxAverage,
                request.SortOrder,
                request.RecommendedShare),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateDescriptiveRatingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateDescriptiveRatingCommand(
                id,
                request.Code,
                request.Name,
                request.MinAverage,
                request.MaxAverage,
                request.SortOrder,
                request.RecommendedShare,
                request.IsActive),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
