namespace VariableCompensation.Application.Abstractions.Notifications;

public interface IEvaluationNotificationService
{
    Task NotifySubmittedForReviewAsync(long evaluationId, CancellationToken cancellationToken);

    Task NotifyApprovedAsync(long evaluationId, CancellationToken cancellationToken);

    Task NotifyReturnedForRevisionAsync(long evaluationId, string revisionComment, CancellationToken cancellationToken);
}
