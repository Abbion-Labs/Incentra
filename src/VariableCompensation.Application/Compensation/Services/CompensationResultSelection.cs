using VariableCompensation.Domain.Entities.Compensation;

namespace VariableCompensation.Application.Compensation.Services;

public static class CompensationResultSelection
{
    public static IReadOnlyList<VariableCompensationResult> LatestPerEmployee(
        IEnumerable<VariableCompensationResult> results)
    {
        return results
            .GroupBy(r => r.EmployeeId)
            .Select(group => group
                .OrderByDescending(r => r.CalculatedAt)
                .ThenByDescending(r => r.IsFinal)
                .ThenByDescending(r => r.Id)
                .First())
            .OrderBy(r => r.Employee.LastName)
            .ThenBy(r => r.Employee.FirstName)
            .ToList();
    }
}
