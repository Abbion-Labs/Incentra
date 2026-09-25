using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Compensation;
using VariableCompensation.Application.Compensation.Commands;
using VariableCompensation.Application.Compensation.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/compensation-parameters")]
public sealed class CompensationParametersController : ControllerBase
{
    private readonly IMediator mediator;

    public CompensationParametersController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "PAYROLL")]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? organizationUnitId,
        [FromQuery] short? year,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetCompensationParametersQuery(organizationUnitId, year, isActive), cancellationToken);
        return this.Ok(result);
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "PAYROLL")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetCompensationParametersByIdQuery(id), cancellationToken);
        return result is null ? this.NotFound() : this.Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "PAYROLL")]
    public async Task<IActionResult> Create([FromBody] CreateCompensationParametersRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new CreateCompensationParametersCommand(
                request.OrganizationUnitId,
                request.Year,
                request.MonetaryPool,
                request.Currency,
                request.AcceptablePerformanceRating,
                request.DependencyWeight,
                request.Exponent,
                request.AllowNegativeVariable),
            cancellationToken);

        return result.IsSuccess
            ? this.CreatedAtAction(nameof(this.GetById), new { id = result.Value.Id }, result.Value)
            : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "PAYROLL")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCompensationParametersRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateCompensationParametersCommand(
                id,
                request.MonetaryPool,
                request.Currency,
                request.AcceptablePerformanceRating,
                request.DependencyWeight,
                request.Exponent,
                request.AllowNegativeVariable,
                request.IsActive,
                request.Version),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:long}/calculation-status")]
    [Authorize(Roles = "PAYROLL")]
    public async Task<IActionResult> GetCalculationStatus(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetCompensationCalculationStatusQuery(id), cancellationToken);
        return result is null ? this.NotFound() : this.Ok(result);
    }

    [HttpPost("{id:long}/calculate")]
    [Authorize(Roles = "PAYROLL")]
    public async Task<IActionResult> Calculate(long id, [FromBody] CalculateCompensationRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new CalculateVariableCompensationCommand(id, request.IsFinal, request.RequireAllQuarters),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:long}/finalize")]
    [Authorize(Roles = "PAYROLL")]
    public async Task<IActionResult> Finalize(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new FinalizeCompensationResultsCommand(id), cancellationToken);
        return result.IsSuccess ? this.Ok(new { finalizedCount = result.Value }) : this.BadRequest(new { error = result.Error });
    }
}
