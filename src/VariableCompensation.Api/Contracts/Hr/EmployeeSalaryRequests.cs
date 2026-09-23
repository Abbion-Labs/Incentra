namespace VariableCompensation.Api.Contracts.Hr;

public sealed class UpsertEmployeeSalaryRequest
{
    /// <summary>
    /// The version of the salary in force the edit was made from, left out only when the employee has no salary
    /// yet. An edit from an outdated copy is refused.
    /// </summary>
    public int? Version { get; init; }

    public int Points { get; init; }

    public decimal SalaryPerPoint { get; init; }

    public DateOnly EffectiveFrom { get; init; }

    public string Currency { get; init; } = "RSD";
}
