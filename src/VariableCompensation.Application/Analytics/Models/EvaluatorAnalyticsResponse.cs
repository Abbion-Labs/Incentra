namespace VariableCompensation.Application.Analytics.Models;

public sealed class EvaluatorAnalyticsResponse
{
    public long EvaluatorEmployeeId { get; init; }

    public string EvaluatorFullName { get; init; } = string.Empty;

    public short Year { get; init; }

    public short PreviousYear { get; init; }

    public int SubordinateCount { get; init; }

    public int RatedEvaluationsThisYear { get; init; }

    public IReadOnlyList<DescriptiveRatingDistributionItem> DistributionThisYear { get; init; } = [];

    public IReadOnlyList<DescriptiveRatingComparisonItem> RatingComparison { get; init; } = [];

    public OverallStatsComparison OverallStats { get; init; } = new();
}

public sealed class DescriptiveRatingDistributionItem
{
    public long DescriptiveRatingId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int Count { get; init; }

    public decimal Percentage { get; init; }

    public int SortOrder { get; init; }
}

public sealed class DescriptiveRatingComparisonItem
{
    public long DescriptiveRatingId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int PreviousYearCount { get; init; }

    public int ThisYearCount { get; init; }

    public int RecommendedCount { get; init; }

    public int SortOrder { get; init; }
}

public sealed class OverallStatsComparison
{
    public EvaluatorPeriodStats SelectedYear { get; init; } = new();

    public EvaluatorPeriodStats AllYears { get; init; } = new();
}

public sealed class EvaluatorPeriodStats
{
    public decimal? Average { get; init; }

    public decimal? Median { get; init; }

    public decimal? Variance { get; init; }
}
