using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Hr;
using VariableCompensation.Application.Hr.EvaluatorSettings.Commands;
using VariableCompensation.Application.Hr.EvaluatorSettings.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/evaluator-settings")]
public sealed class EvaluatorSettingsController : ControllerBase
{
    private readonly IMediator mediator;

    public EvaluatorSettingsController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetEvaluatorSettingsListQuery(), cancellationToken);
        return this.Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Roles = "ADMIN,EVALUATOR")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetMyEvaluatorSettingsQuery(), cancellationToken);
        return result is null ? this.NotFound() : this.Ok(result);
    }

    [HttpGet("my-evaluators")]
    [Authorize(Roles = "ADMIN,CONTROLLER")]
    public async Task<IActionResult> GetMyEvaluators(CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetEvaluatorsForControllerQuery(), cancellationToken);
        return this.Ok(result);
    }

    [HttpGet("{employeeId:long}")]
    [Authorize(Roles = "ADMIN,EVALUATOR")]
    public async Task<IActionResult> GetByEmployeeId(long employeeId, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetEvaluatorSettingsByEmployeeIdQuery(employeeId), cancellationToken);
        return result is null ? this.NotFound() : this.Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateEvaluatorSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new CreateEvaluatorSettingsCommand(
                request.EmployeeId,
                request.ControllerEmployeeId,
                request.ThresholdDoesNotMeet,
                request.ThresholdMeets,
                request.ThresholdGood,
                request.ThresholdExceeds,
                request.PercentDoesNotMeet,
                request.PercentMeets,
                request.PercentGood,
                request.PercentExceeds),
            cancellationToken);

        return result.IsSuccess
            ? this.CreatedAtAction(nameof(this.GetByEmployeeId), new { employeeId = result.Value.EmployeeId }, result.Value)
            : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{employeeId:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(long employeeId, [FromBody] UpdateEvaluatorSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateEvaluatorSettingsCommand(
                employeeId,
                request.ControllerEmployeeId,
                request.ThresholdDoesNotMeet,
                request.ThresholdMeets,
                request.ThresholdGood,
                request.ThresholdExceeds,
                request.PercentDoesNotMeet,
                request.PercentMeets,
                request.PercentGood,
                request.PercentExceeds),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
