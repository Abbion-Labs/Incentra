namespace VariableCompensation.Testing.Common;

public static class TestCredentials
{
    public const string AdminEmail = "admin@local.dev";
    public const string AdminPassword = "Admin123!";
    public const string EvaluatorEmail = "evaluator@local.dev";
    public const string EvaluatorPassword = "Eval123!";
    public const string ControllerEmail = "controller@local.dev";
    public const string ControllerPassword = "Control123!";
    public const string EmployeeEmail = "marko@local.dev";
    public const string EmployeePassword = "Marko123!";
    public const string PayrollEmail = "payroll@local.dev";
    public const string PayrollPassword = "Payroll123!";
}

public static class TestEmployeeIds
{
    public static long Evaluator { get; set; } = 2;
    public static long Controller { get; set; } = 3;
    public static long Employee { get; set; } = 10;
}
