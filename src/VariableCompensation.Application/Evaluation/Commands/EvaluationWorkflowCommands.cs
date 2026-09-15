using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Notifications;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Evaluation.Commands;

public sealed record SubmitEvaluationCommand(long Id, int Version) : IRequest<Result<EvaluationDetailResponse>>;

public sealed class SubmitEvaluationCommandHandler : IRequestHandler<SubmitEvaluationCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IEvaluationLookupRepository lookupRepository;
    private readonly EvaluationScoringService scoringService;
    private readonly ICurrentUserService currentUserService;
    private readonly EvaluationAccessService evaluationAccessService;
    private readonly IEvaluationNotificationService notificationService;
    private readonly ILogger<SubmitEvaluationCommandHandler> logger;

    public SubmitEvaluationCommandHandler(
        IEvaluationRepository evaluationRepository,
        IEvaluationLookupRepository lookupRepository,
        EvaluationScoringService scoringService,
        ICurrentUserService currentUserService,
        EvaluationAccessService evaluationAccessService,
        IEvaluationNotificationService notificationService,
        ILogger<SubmitEvaluationCommandHandler> logger)
    {
        this.evaluationRepository = evaluationRepository;
        this.lookupRepository = lookupRepository;
        this.scoringService = scoringService;
        this.currentUserService = currentUserService;
        this.evaluationAccessService = evaluationAccessService;
        this.notificationService = notificationService;
        this.logger = logger;
    }

    public async Task<Result<EvaluationDetailResponse>> Handle(SubmitEvaluationCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EvaluationNotFound);
        }

        if (entity.Version != request.Version)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EvaluationVersionConflict);
        }

        var submitAccess = await this.evaluationAccessService.EnsureCanSubmitAsync(entity, cancellationToken);
        if (submitAccess.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(submitAccess.Error);
        }

        if (entity.Goals.Count == 0)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.SubmitRequiresGoal);
        }

        if (entity.ConditionsFulfilled)
        {
            if (entity.Measures.Count == 0)
            {
                return Result.Failure<EvaluationDetailResponse>(ErrorCodes.SubmitRequiresMeasure);
            }
        }
        else if (string.IsNullOrWhiteSpace(entity.ConditionsNotMetComment))
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.SubmitRequiresEvaluatorComment);
        }

        var ratingLevels = await this.lookupRepository.GetRatingLevelsAsync(cancellationToken);
        if (entity.ConditionsFulfilled && RatingLevelRules.HasIncompleteRatings(entity, ratingLevels))
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.SubmitIncompleteRatingsNotAllowed);
        }

        if (!EvaluationWorkflow.TryGetNextStatus(entity.Status, EvaluationWorkflowAction.Submit, out var next, out var error))
        {
            return Result.Failure<EvaluationDetailResponse>(error!);
        }

        var descriptiveRatings = await this.lookupRepository.GetDescriptiveRatingsAsync(cancellationToken);
        this.scoringService.Recalculate(entity, ratingLevels, descriptiveRatings);

        var userId = this.currentUserService.UserId ?? 0;
        EvaluationWorkflow.ApplyTransition(entity, entity.Status, next, userId, null);
        entity.ControllerViewedAt = null;
        entity.UpdatedByUserId = this.currentUserService.UserId;

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await this.notificationService.NotifySubmittedForReviewAsync(entity.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send submission notification for evaluation {EvaluationId}", entity.Id);
        }

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}

public sealed record StartReviewEvaluationCommand(long Id, int Version) : IRequest<Result<EvaluationDetailResponse>>;

public sealed class StartReviewEvaluationCommandHandler : IRequestHandler<StartReviewEvaluationCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly EvaluationAccessService evaluationAccessService;

    public StartReviewEvaluationCommandHandler(
        IEvaluationRepository evaluationRepository,
        ICurrentUserService currentUserService,
        EvaluationAccessService evaluationAccessService)
    {
        this.evaluationRepository = evaluationRepository;
        this.currentUserService = currentUserService;
        this.evaluationAccessService = evaluationAccessService;
    }

    public async Task<Result<EvaluationDetailResponse>> Handle(StartReviewEvaluationCommand request, CancellationToken cancellationToken)
    {
        return await this.TransitionAsync(
            request.Id,
            request.Version,
            EvaluationWorkflowAction.StartReview,
            null,
            cancellationToken);
    }

    private async Task<Result<EvaluationDetailResponse>> TransitionAsync(
        long id,
        int version,
        EvaluationWorkflowAction action,
        string? comment,
        CancellationToken cancellationToken)
    {
        var entity = await this.evaluationRepository.FindByIdForUpdateAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EvaluationNotFound);
        }

        if (entity.Version != version)
        {
            return Result.Failure<EvaluationDetailResponse>(ErrorCodes.EvaluationVersionConflict);
        }

        var reviewAccess = await this.evaluationAccessService.EnsureCanReviewAsync(entity, cancellationToken);
        if (reviewAccess.IsFailure)
        {
            return Result.Failure<EvaluationDetailResponse>(reviewAccess.Error);
        }

        if (!EvaluationWorkflow.TryGetNextStatus(entity.Status, action, out var next, out var error))
        {
            return Result.Failure<EvaluationDetailResponse>(error!);
        }

        var from = entity.Status;
        var userId = this.currentUserService.UserId ?? 0;
        EvaluationWorkflow.ApplyTransition(entity, from, next, userId, comment);
        entity.UpdatedByUserId = this.currentUserService.UserId;

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}

public sealed record ApproveEvaluationCommand(long Id, int Version, string? ControllerComment) : IRequest<Result<EvaluationDetailResponse>>;

public sealed class ApproveEvaluationCommandHandler : IRequestHandler<ApproveEvaluationCommand, Result<EvaluationDetailResponse>>
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly EvaluationAccessService evaluationAccessService;
    private readonly IEvaluationNotificationService notificationService;
    private readonly ILogger<ApproveEvaluationCommandHandler> logger;

    public ApproveEvaluationCommandHandler(
        IEvaluationRepository evaluationRepository,
        ICurrentUserService currentUserService,
        EvaluationAccessService evaluationAccessService,
        IEvaluationNotificationService notificationService,
        ILogger<ApproveEvaluationCommandHandler> logger)
    {
        this.evaluationRepository = evaluationRepository;
        this.currentUserService = currentUserService;
        this.evaluationAccessService = evaluationAccessService;
        this.notificationService = notificationService;
        this.logger = logger;
    }

    public async Task<Result<EvaluationDetailResponse>> Handle(ApproveEvaluationCommand request, CancellationToken cancellationToken)
    {
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

        if (!EvaluationWorkflow.TryGetNextStatus(entity.Status, EvaluationWorkflowAction.Approve, out var next, out var error))
        {
            return Result.Failure<EvaluationDetailResponse>(error!);
        }

        var from = entity.Status;
        var userId = this.currentUserService.UserId ?? 0;
        entity.ControllerComment = request.ControllerComment;
        entity.ExcludedFromCompensation = !entity.ConditionsFulfilled;
        EvaluationWorkflow.ApplyTransition(entity, from, next, userId, request.ControllerComment);
        entity.UpdatedByUserId = this.currentUserService.UserId;

        await this.evaluationRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await this.notificationService.NotifyApprovedAsync(entity.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send approval notification for evaluation {EvaluationId}", entity.Id);
        }

        var updated = await this.evaluationRepository.FindByIdAsync(entity.Id, cancellationToken);
        return Result.Success(EvaluationMappings.ToDetail(updated!));
    }
}
