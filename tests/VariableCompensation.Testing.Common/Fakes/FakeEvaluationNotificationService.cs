using VariableCompensation.Application.Abstractions.Notifications;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeEvaluationNotificationService : IEvaluationNotificationService
{
    public List<string> Calls { get; } = [];

    public Task NotifySubmittedForReviewAsync(long evaluationId, CancellationToken cancellationToken)
    {
        this.Calls.Add($"submitted:{evaluationId}");
        return Task.CompletedTask;
    }

    public Task NotifyApprovedAsync(long evaluationId, CancellationToken cancellationToken)
    {
        this.Calls.Add($"approved:{evaluationId}");
        return Task.CompletedTask;
    }

    public Task NotifyReturnedForRevisionAsync(long evaluationId, string revisionComment, CancellationToken cancellationToken)
    {
        this.Calls.Add($"returned:{evaluationId}:{revisionComment}");
        return Task.CompletedTask;
    }
}
