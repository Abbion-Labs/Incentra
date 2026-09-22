using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Application.Evaluation.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lookups")]
public sealed class EvaluationLookupsController : ControllerBase
{
    private readonly IMediator mediator;

    public EvaluationLookupsController(IMediator mediator) => this.mediator = mediator;

    [HttpGet("rating-levels")]
    public async Task<IActionResult> GetRatingLevels(CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetRatingLevelsQuery(), cancellationToken);
        return this.Ok(result);
    }

    [HttpGet("descriptive-ratings")]
    public async Task<IActionResult> GetDescriptiveRatings(CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetDescriptiveRatingsQuery(), cancellationToken);
        return this.Ok(result);
    }

    [HttpGet("measure-types")]
    public async Task<IActionResult> GetMeasureTypes(CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetMeasureTypesQuery(), cancellationToken);
        return this.Ok(result);
    }
}
