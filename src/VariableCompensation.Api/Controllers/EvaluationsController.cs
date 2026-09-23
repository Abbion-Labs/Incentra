using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Evaluation;
using VariableCompensation.Application.Evaluation.Commands;
using VariableCompensation.Application.Evaluation.Queries;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/evaluations")]
public sealed class EvaluationsController : ControllerBase
{
    private readonly IMediator mediator;

    public EvaluationsController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] short? year = null,
        [FromQuery] byte? quarter = null,
        [FromQuery] EvaluationStatus? status = null,
        [FromQuery] string? bucket = null,
        [FromQuery] string? search = null,
        [FromQuery] long? employeeId = null,
        [FromQuery] long? evaluatorEmployeeId = null,
        [FromQuery] long? controllerEmployeeId = null,
        [FromQuery] long? organizationUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetEvaluationsQuery(page, pageSize, year, quarter, status, bucket, search, employeeId, evaluatorEmployeeId, controllerEmployeeId, organizationUnitId),
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet("bucket-counts")]
    public async Task<IActionResult> GetBucketCounts(
        [FromQuery] short? year = null,
        [FromQuery] byte? quarter = null,
        [FromQuery] string? search = null,
        [FromQuery] long? employeeId = null,
        [FromQuery] long? evaluatorEmployeeId = null,
        [FromQuery] long? controllerEmployeeId = null,
        [FromQuery] long? organizationUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await this.mediator.Send(
            new GetEvaluationBucketCountsQuery(year, quarter, search, employeeId, evaluatorEmployeeId, controllerEmployeeId, organizationUnitId),
            cancellationToken);

        return this.Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetEvaluationByIdQuery(id), cancellationToken);
        return result is null ? this.NotFound() : this.Ok(result);
    }

    [HttpGet("{id:long}/status-history")]
    public async Task<IActionResult> GetStatusHistory(long id, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetEvaluationStatusHistoryQuery(id), cancellationToken);
        return this.Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> Create([FromBody] CreateEvaluationRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new CreateEvaluationCommand(request.EmployeeId, request.Year, request.Quarter),
            cancellationToken);

        return result.IsSuccess
            ? this.CreatedAtAction(nameof(this.GetById), new { id = result.Value.Id }, result.Value)
            : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> UpdateDraft(long id, [FromBody] UpdateEvaluationDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateEvaluationDraftCommand(id, request.Version, request.ConversationAt, request.EvaluatorComment, request.ConditionsNotMetComment, request.ConditionsFulfilled),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}/planning-draft")]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> SavePlanningDraft(long id, [FromBody] SaveEvaluationPlanningDraftRequest request, CancellationToken cancellationToken)
    {
        var goals = request.Goals.Select(g => new EvaluationGoalItem(g.Description, g.RatingLevelId, g.Comment, g.Weight, g.SortOrder, g.Id)).ToList();
        var conditions = request.Conditions.Select(c => new EvaluationConditionItem(c.Description, c.SortOrder)).ToList();
        var criteria = request.Criteria.Select(c => new EvaluationCriterionItem(c.Description, c.SortOrder)).ToList();
        var result = await this.mediator.Send(
            new SaveEvaluationPlanningDraftCommand(
                id,
                request.Version,
                request.ConversationAt,
                request.EvaluatorComment,
                request.ConditionsNotMetComment,
                request.ConditionsFulfilled,
                goals,
                conditions,
                criteria),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}/rating-draft")]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> SaveRatingDraft(long id, [FromBody] SaveEvaluationRatingDraftRequest request, CancellationToken cancellationToken)
    {
        var goals = request.Goals?.Select(g => new EvaluationGoalItem(g.Description, g.RatingLevelId, g.Comment, g.Weight, g.SortOrder, g.Id)).ToList();
        var measures = request.Measures?.Select(m => new EvaluationMeasureItem(
            m.MeasureTypeId, m.RatingComment, m.RatingLevelId, m.SortOrder)).ToList();
        EvaluationTrainingDraftItem? training = request.Training is null
            ? null
            : new EvaluationTrainingDraftItem(
                request.Training.TrainingDescription,
                request.Training.KnowledgeDescription,
                request.Training.DevelopmentDescription,
                request.Training.EvaluatorComment);

        var result = await this.mediator.Send(
            new SaveEvaluationRatingDraftCommand(
                id,
                request.Version,
                request.ConversationAt,
                request.EvaluatorComment,
                request.ConditionsNotMetComment,
                request.ConditionsFulfilled,
                goals,
                measures,
                training),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}/goals")]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> ReplaceGoals(long id, [FromBody] ReplaceEvaluationGoalsRequest request, CancellationToken cancellationToken)
    {
        var goals = request.Goals.Select(g => new EvaluationGoalItem(g.Description, g.RatingLevelId, g.Comment, g.Weight, g.SortOrder, g.Id)).ToList();
        var result = await this.mediator.Send(new ReplaceEvaluationGoalsCommand(id, request.Version, goals), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}/measures")]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> ReplaceMeasures(long id, [FromBody] ReplaceEvaluationMeasuresRequest request, CancellationToken cancellationToken)
    {
        var measures = request.Measures.Select(m => new EvaluationMeasureItem(
            m.MeasureTypeId, m.RatingComment, m.RatingLevelId, m.SortOrder)).ToList();
        var result = await this.mediator.Send(new ReplaceEvaluationMeasuresCommand(id, request.Version, measures), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}/criteria")]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> ReplaceCriteria(long id, [FromBody] ReplaceEvaluationCriteriaRequest request, CancellationToken cancellationToken)
    {
        var criteria = request.Criteria.Select(c => new EvaluationCriterionItem(c.Description, c.SortOrder)).ToList();
        var result = await this.mediator.Send(new ReplaceEvaluationCriteriaCommand(id, request.Version, criteria), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}/conditions")]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> ReplaceConditions(long id, [FromBody] ReplaceEvaluationConditionsRequest request, CancellationToken cancellationToken)
    {
        var conditions = request.Conditions.Select(c => new EvaluationConditionItem(c.Description, c.SortOrder)).ToList();
        var result = await this.mediator.Send(new ReplaceEvaluationConditionsCommand(id, request.Version, conditions), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}/training")]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> UpsertTraining(long id, [FromBody] UpsertEvaluationTrainingRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpsertEvaluationTrainingCommand(
                id,
                request.Version,
                request.TrainingDescription,
                request.KnowledgeDescription,
                request.DevelopmentDescription,
                request.EvaluatorComment),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:long}/submit")]
    [Authorize(Roles = "EVALUATOR")]
    public async Task<IActionResult> Submit(long id, [FromBody] EvaluationVersionRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new SubmitEvaluationCommand(id, request.Version), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:long}/start-review")]
    [Authorize(Roles = "CONTROLLER")]
    public async Task<IActionResult> StartReview(long id, [FromBody] EvaluationVersionRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new StartReviewEvaluationCommand(id, request.Version), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:long}/approve")]
    [Authorize(Roles = "CONTROLLER")]
    public async Task<IActionResult> Approve(long id, [FromBody] ApproveEvaluationRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new ApproveEvaluationCommand(id, request.Version, request.ControllerComment), cancellationToken);
        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:long}/return-for-revision")]
    [Authorize(Roles = "CONTROLLER")]
    public async Task<IActionResult> ReturnForRevision(long id, [FromBody] ReturnEvaluationForRevisionRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new ReturnEvaluationForRevisionCommand(id, request.Version, request.RevisionComment),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }
}
