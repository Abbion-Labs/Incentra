using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Notifications;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;

namespace VariableCompensation.Application.Evaluation.Commands;

public sealed record ReturnEvaluationForRevisionCommand(long Id, int Version, string RevisionComment)
    : IRequest<Result<EvaluationDetailResponse>>;

public sealed class ReturnEvaluationForRevisionCommandHandler : IRequestHandler<ReturnEvaluationForRevisionCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly EvaluationAccessService evaluationAccessService;
    private readonly IEvaluationNotificationService notificationService;
    private readonly ILogger<ReturnEvaluationForRevisionCommandHandler> logger;

    public ReturnEvaluationForRevisionCommandHandler(
        IEvaluationRepository evaluationRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        EvaluationAccessService evaluationAccessService,
        IEvaluationNotificationService notificationService,
        ILogger<ReturnEvaluationForRevisionCommandHandler> logger)
    {
        this.evaluationRepository = evaluationRepository;
        this.employeeRepository = employeeRepository;
        this.currentUserService = currentUserService;
        this.evaluationAccessService = evaluationAccessService;
        this.notificationService = notificationService;
        this.logger = logger;
    }

    public async Task<Result<EvaluationDetailResponse>> Handle(ReturnEvaluationForRevisionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RevisionComment))
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.RevisionCommentRequired);
        }

        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EvaluationNotFound);
        }

        if (entity.Version != request.Version)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EvaluationVersionConflict);
        }

        var reviewAccess = await this.evaluationAccessService.EnsureCanReviewAsync(entity, cancellationToken);
        if (reviewAccess.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(reviewAccess.Error);
        }

        if (!EvaluationWorkflow.TryGetNextStatus(entity.Status, EvaluationWorkflowAction.ReturnForRevision, out var next, out var error))
        {
            return Result.Failure<EvaluationDetailResponse>(error!);
        }

        var from = entity.Status;
        var userId = this.currentUserService.UserId ?? 0;
        entity.ControllerComment = request.RevisionComment.Trim();
        EvaluationWorkflow.ApplyTransition(
            entity, from, next, userId, this.currentUserService.ActiveRole, request.RevisionComment.Trim());
        entity.UpdatedByUserId = this.currentUserService.UserId;

        // Written by an evaluator who has since handed the employee over: the revision is for whoever rates the
        // employee now.
        var employee = await this.employeeRepository.FindByIdWithEvaluatorAsync(entity.EmployeeId, cancellationToken);
        if (employee?.EvaluatorEmployeeId is { } currentEvaluatorId && currentEvaluatorId != entity.EvaluatorEmployeeId)
        {
            var controllerId = await this.evaluationRepository.GetControllerEmployeeIdAsync(currentEvaluatorId, cancellationToken);
            EvaluationReassignment.Assign(entity, currentEvaluatorId, controllerId);
        }

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        var revisionComment = request.RevisionComment.Trim();
        try
        {
            await this.notificationService.NotifyReturnedForRevisionAsync(entity.Id, revisionComment, cancellationToken);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send return notification for evaluation {EvaluationId}", entity.Id);
        }

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}
