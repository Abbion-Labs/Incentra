using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Evaluation.Services;

internal static class EvaluationWorkflow
{
    public static bool CanEdit(EvaluationStatus status) => status == EvaluationStatus.Draft;

    public static bool TryGetNextStatus(EvaluationStatus current, EvaluationWorkflowAction action, out EvaluationStatus next, out string? error)
    {
        next = current;
        error = null;

        var allowed = (current, action) switch
        {
            (EvaluationStatus.Draft, EvaluationWorkflowAction.Submit) => EvaluationStatus.Submitted,
            (EvaluationStatus.Submitted, EvaluationWorkflowAction.StartReview) => EvaluationStatus.UnderReview,
            (EvaluationStatus.UnderReview, EvaluationWorkflowAction.Approve) => EvaluationStatus.Approved,
            (EvaluationStatus.Submitted, EvaluationWorkflowAction.ReturnForRevision) => EvaluationStatus.Draft,
            (EvaluationStatus.UnderReview, EvaluationWorkflowAction.ReturnForRevision) => EvaluationStatus.Draft,
            _ => (EvaluationStatus?)null
        };

        if (allowed is null)
        {
            error = ErrorCodes.EvaluationActionNotAllowed;
            return false;
        }

        next = allowed.Value;
        return true;
    }

    public static void ApplyTransition(
        EvaluationEntity evaluation,
        EvaluationStatus from,
        EvaluationStatus to,
        long changedByUserId,
        string? changedByRoleCode,
        string? comment)
    {
        evaluation.Status = to;
        evaluation.UpdatedAt = DateTime.UtcNow;
        evaluation.Version++;
        evaluation.StatusHistory.Add(new EvaluationStatusHistory
        {
            FromStatus = from.ToString(),
            ToStatus = to.ToString(),
            ChangedByUserId = changedByUserId,
            ChangedByRoleCode = changedByRoleCode,
            Comment = comment,
            ChangedAt = DateTime.UtcNow
        });

        switch (to)
        {
            case EvaluationStatus.Submitted:
                evaluation.SubmittedAt = DateTime.UtcNow;
                // The comment of a return for revision has been dealt with once the evaluation is sent again; it
                // stays in RejectionReason and in the history, but must not pass for the controller's final word.
                evaluation.ControllerComment = null;
                break;
            case EvaluationStatus.UnderReview:
                evaluation.ReviewedAt = DateTime.UtcNow;
                break;
            case EvaluationStatus.Approved:
                evaluation.ApprovedAt = DateTime.UtcNow;
                break;
            case EvaluationStatus.Draft when from is EvaluationStatus.Submitted or EvaluationStatus.UnderReview:
                evaluation.SubmittedAt = null;
                evaluation.ReviewedAt = null;
                evaluation.ApprovedAt = null;
                evaluation.ExcludedFromCompensation = false;
                evaluation.ControllerComment = comment;
                evaluation.RejectionReason = comment;
                break;
        }
    }
}

internal enum EvaluationWorkflowAction
{
    Submit,
    StartReview,
    Approve,
    ReturnForRevision
}
