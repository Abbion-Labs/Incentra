using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Application.Common;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Evaluation.Services;

public static class EvaluationBucketFilter
{
    public static bool TryParseBucket(string? bucket, out string normalizedBucket)
    {
        normalizedBucket = (bucket ?? string.Empty).Trim().ToLowerInvariant();
        return normalizedBucket switch
        {
            "planning" or "unrated" or "returned" or "submitted" or "approved" or "pending" or "goalscomplete" => true,
            _ => false,
        };
    }

    public static IQueryable<EvaluationEntity> ApplyBucket(IQueryable<EvaluationEntity> query, string bucket)
    {
        var normalized = bucket.Trim().ToLowerInvariant();
        return normalized switch
        {
            "planning" => query.Where(e =>
                e.Status == EvaluationStatus.Draft &&
                !(e.Goals.Any(goal => goal.Description != null && goal.Description.Trim() != string.Empty) &&
                  e.Conditions.Any(condition => condition.Description != null && condition.Description.Trim() != string.Empty) &&
                  e.Criteria.Any(criterion => criterion.Description != null && criterion.Description.Trim() != string.Empty))),
            "unrated" => query.Where(e =>
                e.Status == EvaluationStatus.Draft &&
                e.Goals.Any(goal => goal.Description != null && goal.Description.Trim() != string.Empty) &&
                e.Conditions.Any(condition => condition.Description != null && condition.Description.Trim() != string.Empty) &&
                e.Criteria.Any(criterion => criterion.Description != null && criterion.Description.Trim() != string.Empty) &&
                (e.ControllerComment == null || e.ControllerComment.Trim() == string.Empty)),
            "returned" => query.Where(e =>
                e.Status == EvaluationStatus.Draft &&
                e.ControllerComment != null &&
                e.ControllerComment.Trim() != string.Empty),
            "submitted" or "pending" => query.Where(e =>
                e.Status == EvaluationStatus.Submitted || e.Status == EvaluationStatus.UnderReview),
            "approved" => query.Where(e => e.Status == EvaluationStatus.Approved),
            "goalscomplete" => EmployeeGoalsFilter.WhereGoalsComplete(query),
            _ => query,
        };
    }

    public static IQueryable<EvaluationEntity> ApplySearch(IQueryable<EvaluationEntity> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var term = EmployeeNameSearch.Normalize(search)!;
        return query.Where(e =>
            (e.Employee.FirstName + " " + e.Employee.LastName).ToLower().Contains(term) ||
            (e.Employee.LastName + " " + e.Employee.FirstName).ToLower().Contains(term) ||
            (e.Evaluator.FirstName + " " + e.Evaluator.LastName).ToLower().Contains(term) ||
            (e.Evaluator.LastName + " " + e.Evaluator.FirstName).ToLower().Contains(term));
    }
}
