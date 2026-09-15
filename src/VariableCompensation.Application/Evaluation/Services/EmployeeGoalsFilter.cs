using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using EmployeeEntity = VariableCompensation.Domain.Entities.Hr.Employee;

namespace VariableCompensation.Application.Evaluation.Services;

public static class EmployeeGoalsFilter
{
    public static bool TryParseGoalsBucket(string? bucket, out string normalizedBucket)
    {
        normalizedBucket = (bucket ?? string.Empty).Trim().ToLowerInvariant();
        return normalizedBucket switch
        {
            "pending" => true,
            _ => false,
        };
    }

    public static IQueryable<EvaluationEntity> WhereGoalsComplete(IQueryable<EvaluationEntity> query) =>
        query.Where(e =>
            e.Goals.Any(goal => goal.Description != null && goal.Description.Trim() != string.Empty) &&
            e.Conditions.Any(condition => condition.Description != null && condition.Description.Trim() != string.Empty) &&
            e.Criteria.Any(criterion => criterion.Description != null && criterion.Description.Trim() != string.Empty));

    public static IQueryable<EmployeeEntity> ApplyPendingForPeriod(
        IQueryable<EmployeeEntity> employees,
        IQueryable<EvaluationEntity> evaluations,
        short year,
        byte quarter) =>
        employees.Where(e =>
            !evaluations.Any(ev =>
                ev.EmployeeId == e.Id &&
                ev.Year == year &&
                ev.Quarter == quarter &&
                ev.Goals.Any(goal => goal.Description != null && goal.Description.Trim() != string.Empty) &&
                ev.Conditions.Any(condition => condition.Description != null && condition.Description.Trim() != string.Empty) &&
                ev.Criteria.Any(criterion => criterion.Description != null && criterion.Description.Trim() != string.Empty)));
}
