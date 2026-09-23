namespace VariableCompensation.Domain.Enums;

public static class RoleCodes
{
    public const string Admin = "ADMIN";
    public const string Evaluator = "EVALUATOR";
    public const string Controller = "CONTROLLER";
    public const string Employee = "EMPLOYEE";

    public const string Payroll = "PAYROLL";

    /// <summary>
    /// The order a session falls back to when the user has not chosen a role:
    /// the first role they hold wins. The frontend offers the roles in the same
    /// order.
    /// </summary>
    public static readonly string[] SessionPriority =
        [Evaluator, Controller, Employee, Payroll, Admin];
}
