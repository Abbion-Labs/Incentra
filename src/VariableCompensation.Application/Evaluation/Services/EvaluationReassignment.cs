using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;

namespace VariableCompensation.Application.Evaluation.Services;

/// <summary>
/// Moves an evaluation that is still open to the evaluator and controller who are responsible for the employee
/// now. Drafts move as soon as the assignment changes. A submitted evaluation stays with the evaluator who wrote
/// it: if it is approved it keeps that record, and if it is returned for revision it moves then. Approved
/// evaluations never move.
/// </summary>
internal static class EvaluationReassignment
{
    public static void Assign(EvaluationEntity evaluation, long evaluatorEmployeeId, long? controllerEmployeeId)
    {
        if (evaluation.EvaluatorEmployeeId == evaluatorEmployeeId
            && evaluation.ControllerEmployeeId == controllerEmployeeId)
        {
            return;
        }

        if (evaluation.ControllerEmployeeId != controllerEmployeeId)
        {
            // The new controller has not opened it yet.
            evaluation.ControllerViewedAt = null;
        }

        evaluation.EvaluatorEmployeeId = evaluatorEmployeeId;
        evaluation.ControllerEmployeeId = controllerEmployeeId;
        evaluation.UpdatedAt = DateTime.UtcNow;

        // An editor still open on the previous assignment must not save over it.
        evaluation.Version++;
    }
}
