using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Hr;
using VariableCompensation.Application.Hr.EmployeeSalaries.Commands;
using VariableCompensation.Application.Hr.EmployeeSalaries.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize(Roles = "PAYROLL")]
[Route("api/employee-salaries")]
public sealed class EmployeeSalariesController : ControllerBase
{
    private readonly IMediator mediator;

    public EmployeeSalariesController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetEmployeeSalariesPagedQuery(page, pageSize, search),
            cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpGet("employees-without-salary")]
    public async Task<IActionResult> GetEmployeesWithoutSalary(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetEmployeesWithoutSalaryPagedQuery(page, pageSize, search),
            cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpGet("{employeeId:long}/history")]
    public async Task<IActionResult> GetHistory(long employeeId, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetEmployeeSalaryHistoryQuery(employeeId), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.NotFound(new { error = result.Error });
    }

    [HttpPut("{employeeId:long}")]
    public async Task<IActionResult> Upsert(long employeeId, [FromBody] UpsertEmployeeSalaryRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpsertEmployeeSalaryCommand(
                employeeId,
                request.Points,
                request.SalaryPerPoint,
                request.EffectiveFrom,
                request.Currency),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
