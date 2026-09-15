namespace VariableCompensation.Application.Hr.Models;

public sealed class EmployeeSalaryResponse
{
    public long Id { get; init; }

    public long EmployeeId { get; init; }

    public string EmployeeFullName { get; init; } = string.Empty;

    public string OrganizationUnitName { get; init; } = string.Empty;

    public int Points { get; init; }

    public decimal SalaryPerPoint { get; init; }

    public string Currency { get; init; } = "RSD";

    public DateOnly EffectiveFrom { get; init; }

    public DateOnly? EffectiveTo { get; init; }

    public bool IsCurrent { get; init; }

    public DateTime UpdatedAt { get; init; }
}
