namespace VariableCompensation.Application.Compensation.Models;

public static class CompensationAnalyticsChartTypes
{
    public const string ShareDistribution = "shareDistribution";
    public const string ShareByEmployee = "shareByEmployee";
    public const string NetDistribution = "netDistribution";
    public const string NetByEmployee = "netByEmployee";
    public const string MonthlyByEmployee = "monthlyByEmployee";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ShareDistribution,
        ShareByEmployee,
        NetDistribution,
        NetByEmployee,
        MonthlyByEmployee,
    };
}

public sealed class CompensationAnalyticsResponse
{
    public string ChartType { get; init; } = string.Empty;

    public short Year { get; init; }

    public string Currency { get; init; } = "RSD";

    public string? OrganizationUnitName { get; set; }

    public string? OrganizationUnitKey { get; set; }

    public string ValueFormat { get; init; } = "count";

    public IReadOnlyList<CompensationAnalyticsBucketResponse> Buckets { get; init; } =
        Array.Empty<CompensationAnalyticsBucketResponse>();

    public IReadOnlyList<CompensationAnalyticsSeriesPointResponse> Series { get; init; } =
        Array.Empty<CompensationAnalyticsSeriesPointResponse>();
}

public sealed class CompensationAnalyticsBucketResponse
{
    public string Label { get; init; } = string.Empty;

    public int Count { get; init; }
}

public sealed class CompensationAnalyticsSeriesPointResponse
{
    public string Label { get; init; } = string.Empty;

    public decimal Value { get; init; }
}
