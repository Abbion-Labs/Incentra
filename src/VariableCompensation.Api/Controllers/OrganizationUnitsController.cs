using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Hr;
using VariableCompensation.Application.Hr.OrganizationUnits.Commands;
using VariableCompensation.Application.Hr.OrganizationUnits.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organization-units")]
public sealed class OrganizationUnitsController : ControllerBase
{
    private readonly IMediator mediator;

    public OrganizationUnitsController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetOrganizationUnitsQuery(isActive), cancellationToken);
        return this.Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetOrganizationUnitByIdQuery(id), cancellationToken);
        return result is null ? this.NotFound() : this.Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new CreateOrganizationUnitCommand(request.Name, request.Code), cancellationToken);
        return result.IsSuccess ? this.CreatedAtAction(nameof(this.GetById), new { id = result.Value.Id }, result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateOrganizationUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateOrganizationUnitCommand(id, request.Name, request.Code, request.IsActive, request.Version),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
