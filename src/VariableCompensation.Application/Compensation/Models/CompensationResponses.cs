namespace VariableCompensation.Application.Compensation.Models;

public sealed class CompensationParametersResponse
{
    public long Id { get; init; }

    public long OrganizationUnitId { get; init; }

    public string OrganizationUnitName { get; init; } = string.Empty;

    public short Year { get; init; }

    public decimal MonetaryPool { get; init; }

    public string Currency { get; init; } = "RSD";

    public decimal AcceptablePerformanceRating { get; init; }

    public decimal UpperLimitCoefficient { get; init; }

    public decimal DependencyWeight { get; init; }

    public decimal Exponent { get; init; }

    public bool AllowNegativeVariable { get; init; }

    public bool IsActive { get; init; }
}

public class CompensationResultSummaryResponse
{
    public long Id { get; init; }

    public long EmployeeId { get; init; }

    public string EmployeeFullName { get; init; } = string.Empty;

    public string OrganizationUnitName { get; init; } = string.Empty;

    public long ParametersId { get; init; }

    public short Year { get; init; }

    public decimal OverallAverage { get; init; }

    public int Points { get; init; }

    public decimal SalaryPointsValue { get; init; }

    public decimal FixedSalary { get; init; }

    public decimal CompensationPercent { get; init; }

    public decimal NetCompensation { get; init; }

    public decimal QuarterlyCompensation { get; init; }

    public decimal MonthlyCompensation { get; init; }

    public bool IsFinal { get; init; }

    public DateTime CalculatedAt { get; init; }
}

public sealed class CompensationResultDetailResponse : CompensationResultSummaryResponse
{
    public decimal GoalsAverage { get; init; }

    public decimal MeasuresAverage { get; init; }

    public decimal Weight { get; init; }

    public decimal ZScore { get; init; }

    public decimal NormalizedZScore { get; init; }

    public decimal NormalizedPoints { get; init; }

    public decimal CompensationWithoutSalary { get; init; }

    public decimal Aq { get; init; }

    public string FormulaVersion { get; init; } = string.Empty;

    public IReadOnlyList<CompensationEvaluationLinkResponse> EvaluationLinks { get; init; } =
        Array.Empty<CompensationEvaluationLinkResponse>();
}

public sealed class CompensationEvaluationLinkResponse
{
    public long EvaluationId { get; init; }

    public byte Quarter { get; init; }

    public decimal OverallAverage { get; init; }

    public DateTime? ApprovedAt { get; init; }
}

public sealed class CalculateCompensationResponse
{
    public long ParametersId { get; init; }

    public int EmployeesCalculated { get; init; }

    public int EmployeesSkipped { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public IReadOnlyList<CompensationResultSummaryResponse> Results { get; init; } =
        Array.Empty<CompensationResultSummaryResponse>();
}
