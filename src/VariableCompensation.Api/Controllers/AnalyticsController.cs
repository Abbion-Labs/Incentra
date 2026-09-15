using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Application.Analytics.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize(Roles = "EVALUATOR,ADMIN,CONTROLLER")]
[Route("api/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IMediator mediator;

    public AnalyticsController(IMediator mediator) => this.mediator = mediator;

    [HttpGet("evaluator")]
    public async Task<IActionResult> GetEvaluatorAnalytics(
        [FromQuery] short? year = null,
        [FromQuery] long? evaluatorEmployeeId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(new GetEvaluatorAnalyticsQuery(year, evaluatorEmployeeId), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
