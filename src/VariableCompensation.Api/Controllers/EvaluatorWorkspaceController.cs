using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Application.Analytics.Queries;
using VariableCompensation.Application.Evaluation.Queries;
using VariableCompensation.Application.Hr.Employees.Queries;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Api.Controllers;

/// <summary>
/// Lists for the evaluator screens. The session works in one role and the token
/// carries only that role, so these answer for the evaluator the user is signed
/// in as, or for an admin session, for everyone.
/// </summary>
[ApiController]
[Authorize(Roles = "EVALUATOR,ADMIN")]
[Route("api/evaluator")]
public sealed class EvaluatorWorkspaceController : ControllerBase
{
    private readonly IMediator mediator;

    public EvaluatorWorkspaceController(IMediator mediator) => this.mediator = mediator;

    [HttpGet("evaluations")]
    public async Task<IActionResult> GetEvaluations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] short? year = null,
        [FromQuery] byte? quarter = null,
        [FromQuery] EvaluationStatus? status = null,
        [FromQuery] string? bucket = null,
        [FromQuery] string? search = null,
        [FromQuery] long? employeeId = null,
        [FromQuery] long? organizationUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetEvaluationsQuery(
                page,
                pageSize,
                year,
                quarter,
                status,
                bucket,
                search,
                employeeId,
                EvaluatorEmployeeId: null,
                ControllerEmployeeId: null,
                organizationUnitId),
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet("evaluations/bucket-counts")]
    public async Task<IActionResult> GetEvaluationBucketCounts(
        [FromQuery] short? year = null,
        [FromQuery] byte? quarter = null,
        [FromQuery] string? search = null,
        [FromQuery] long? employeeId = null,
        [FromQuery] long? organizationUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetEvaluationBucketCountsQuery(
                year,
                quarter,
                search,
                employeeId,
                EvaluatorEmployeeId: null,
                ControllerEmployeeId: null,
                organizationUnitId),
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] long? organizationUnitId = null,
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
                EvaluatorEmployeeId: null,
                search,
                isActive,
                goalsYear,
                goalsQuarter,
                goalsBucket),
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] short? year = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetEvaluatorAnalyticsQuery(year),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
