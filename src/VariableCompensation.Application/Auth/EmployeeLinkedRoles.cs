using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Auth;

/// <summary>
/// The roles that act as a particular employee: an employee sees their own evaluations, an evaluator rates the
/// people assigned to their employee, and a controller reviews the evaluators assigned to theirs. An account
/// holding one of them is always linked to an employee.
/// </summary>
public static class EmployeeLinkedRoles
{
    public static readonly IReadOnlyList<string> All = [RoleCodes.Employee, RoleCodes.Evaluator, RoleCodes.Controller];

    public static bool AnyIn(IEnumerable<string> roleCodes) =>
        roleCodes.Any(code => All.Contains(code.Trim(), StringComparer.OrdinalIgnoreCase));
}
