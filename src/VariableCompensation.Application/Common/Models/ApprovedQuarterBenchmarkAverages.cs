namespace VariableCompensation.Application.Common.Models;

public sealed record ApprovedQuarterBenchmarkAverages(
    short Year,
    byte Quarter,
    decimal? OverallAverage,
    decimal? GoalsAverage,
    decimal? MeasuresAverage);
