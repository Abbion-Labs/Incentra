using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Hr;
using VariableCompensation.Application.Hr.Employees.Commands;
using VariableCompensation.Application.Hr.Employees.Queries;
using VariableCompensation.Application.Hr.EvaluatorSettings.Commands;
using VariableCompensation.Domain;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class EmployeesController : ControllerBase
{
    private readonly IMediator mediator;

    public EmployeesController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] long? organizationUnitId = null,
        [FromQuery] long? evaluatorEmployeeId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] short? goalsYear = null,
        [FromQuery] byte? goalsQuarter = null,
        [FromQuery] string? goalsBucket = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetEmployeesQuery(
                page,
                pageSize,
                organizationUnitId,
                evaluatorEmployeeId,
                search,
                isActive,
                goalsYear,
                goalsQuarter,
                goalsBucket),
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetEmployeeByIdQuery(id), cancellationToken);
        if (result.IsSuccess)
        {
            return this.Ok(result.Value);
        }

        return result.Error == ErrorCodes.EmployeeNotFound
            ? this.NotFound(new { error = result.Error })
            : this.BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:long}/evaluation-benchmarks")]
    public async Task<IActionResult> GetEvaluationBenchmarks(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetEmployeeEvaluationBenchmarksQuery(id), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new CreateEmployeeCommand(
                request.FirstName,
                request.LastName,
                request.OrganizationUnitId,
                request.JobPositionId,
                request.EducationLevelId,
                request.EvaluatorEmployeeId,
                request.HiredAt),
            cancellationToken);

        return result.IsSuccess ? this.CreatedAtAction(nameof(this.GetById), new { id = result.Value.Id }, result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateEmployeeCommand(
                id,
                request.FirstName,
                request.LastName,
                request.OrganizationUnitId,
                request.JobPositionId,
                request.EducationLevelId,
                request.EvaluatorEmployeeId,
                request.HiredAt,
                request.IsActive),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}/user")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> LinkUser(long id, [FromBody] LinkEmployeeUserRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new LinkEmployeeUserCommand(id, request.UserId), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:long}/avatar")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(long id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return this.BadRequest(new { error = "Image file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await this.mediator.Send(
            new UploadEmployeeAvatarCommand(id, stream, file.ContentType, file.Length),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:long}/avatar")]
    public async Task<IActionResult> DeleteAvatar(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new DeleteEmployeeAvatarCommand(id), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
