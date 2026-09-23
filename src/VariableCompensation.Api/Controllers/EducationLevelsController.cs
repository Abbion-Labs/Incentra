using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Hr;
using VariableCompensation.Application.Hr.EducationLevels.Commands;
using VariableCompensation.Application.Hr.EducationLevels.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/education-levels")]
public sealed class EducationLevelsController : ControllerBase
{
    private readonly IMediator mediator;

    public EducationLevelsController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetEducationLevelsQuery(isActive), cancellationToken);
        return this.Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateEducationLevelRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new CreateEducationLevelCommand(request.Name, request.SortOrder), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEducationLevelRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateEducationLevelCommand(id, request.Name, request.SortOrder, request.IsActive, request.Version),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
