namespace VariableCompensation.Application.Hr.Models;

public sealed class EmployeeEvaluationBenchmarksResponse
{
    public EmployeeResponse Employee { get; init; } = null!;

    public IReadOnlyList<EmployeeQuarterBenchmarkResponse> Quarters { get; init; } = Array.Empty<EmployeeQuarterBenchmarkResponse>();
}

public sealed class EmployeeQuarterBenchmarkResponse
{
    public long EvaluationId { get; init; }

    public short Year { get; init; }

    public byte Quarter { get; init; }

    public string Status { get; init; } = string.Empty;

    public decimal? EmployeeAverage { get; init; }

    public decimal? EmployeeGoalsAverage { get; init; }

    public decimal? EmployeeMeasuresAverage { get; init; }

    public decimal? OrganizationUnitAverage { get; init; }

    public decimal? OrganizationUnitGoalsAverage { get; init; }

    public decimal? OrganizationUnitMeasuresAverage { get; init; }

    public decimal? JobPositionAverage { get; init; }

    public decimal? JobPositionGoalsAverage { get; init; }

    public decimal? JobPositionMeasuresAverage { get; init; }

    public bool HasIncompleteRatings { get; init; }

    public string? DescriptiveRatingName { get; init; }
}
