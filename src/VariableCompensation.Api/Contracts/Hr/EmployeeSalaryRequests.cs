namespace VariableCompensation.Api.Contracts.Hr;

public sealed class UpsertEmployeeSalaryRequest
{
    public int Points { get; init; }

    public decimal SalaryPerPoint { get; init; }

    public DateOnly EffectiveFrom { get; init; }

    public string Currency { get; init; } = "RSD";
}
