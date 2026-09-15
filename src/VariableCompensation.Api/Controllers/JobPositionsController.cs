using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Hr;
using VariableCompensation.Application.Hr.JobPositions.Commands;
using VariableCompensation.Application.Hr.JobPositions.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/job-positions")]
public sealed class JobPositionsController : ControllerBase
{
    private readonly IMediator mediator;

    public JobPositionsController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetJobPositionsQuery(isActive), cancellationToken);
        return this.Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateJobPositionRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new CreateJobPositionCommand(request.Name, request.SortOrder), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateJobPositionRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateJobPositionCommand(id, request.Name, request.SortOrder, request.IsActive),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
