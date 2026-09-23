using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Application.Evaluation.Commands;
using CSharpFunctionalExtensions;
using VariableCompensation.Domain;

namespace VariableCompensation.Application.Evaluation.Services;

public static class EvaluationPlanningRules
{
    public static bool HasDefinedGoals(EvaluationEntity evaluation) =>
        evaluation.Goals.Any(g => !string.IsNullOrWhiteSpace(g.Description));

    public static bool HasDefinedConditions(EvaluationEntity evaluation) =>
        evaluation.Conditions.Any(c => !string.IsNullOrWhiteSpace(c.Description));

    public static bool HasDefinedCriteria(EvaluationEntity evaluation) =>
        evaluation.Criteria.Any(c => !string.IsNullOrWhiteSpace(c.Description));

    public static bool IsGoalsPlanningComplete(EvaluationEntity evaluation) =>
        HasDefinedGoals(evaluation) && HasDefinedConditions(evaluation) && HasDefinedCriteria(evaluation);

    public static Result EnsureConditionsPlanningEditable(EvaluationEntity evaluation)
    {
        if (IsGoalsPlanningComplete(evaluation))
        {
            return Result.Failure(ErrorCodes.GoalsPlanningLocked);
        }

        return Result.Success();
    }

    public static Result EnsureCriteriaPlanningEditable(EvaluationEntity evaluation)
    {
        if (IsGoalsPlanningComplete(evaluation))
        {
            return Result.Failure(ErrorCodes.GoalsPlanningLocked);
        }

        return Result.Success();
    }
}
