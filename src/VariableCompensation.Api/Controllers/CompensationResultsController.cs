using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Application.Compensation.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize(Roles = "PAYROLL")]
[Route("api/compensation-results")]
public sealed class CompensationResultsController : ControllerBase
{
    private readonly IMediator mediator;

    public CompensationResultsController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] long? parametersId = null,
        [FromQuery] short? year = null,
        [FromQuery] long? organizationUnitId = null,
        [FromQuery] long? employeeId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetCompensationResultsQuery(page, pageSize, parametersId, year, organizationUnitId, employeeId, search),
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] short year,
        [FromQuery] string chartType,
        [FromQuery] long? organizationUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetCompensationAnalyticsQuery(year, organizationUnitId, chartType),
            cancellationToken);

        return result is null ? this.BadRequest(new { error = "Invalid chart type or access denied." }) : this.Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetCompensationResultByIdQuery(id), cancellationToken);
        return result is null ? this.NotFound() : this.Ok(result);
    }
}
