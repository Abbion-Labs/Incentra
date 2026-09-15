using VariableCompensation.Application.Compensation.Models;
using VariableCompensation.Domain.Entities.Compensation;

namespace VariableCompensation.Application.Compensation.Services;

public sealed class CompensationAnalyticsService
{
    public CompensationAnalyticsResponse Build(
        string chartType,
        short year,
        string currency,
        string? organizationUnitName,
        IReadOnlyList<VariableCompensationResult> results)
    {
        return chartType switch
        {
            CompensationAnalyticsChartTypes.ShareDistribution => BuildShareDistribution(year, currency, organizationUnitName, results),
            CompensationAnalyticsChartTypes.ShareByEmployee => BuildShareByEmployee(year, currency, organizationUnitName, results),
            CompensationAnalyticsChartTypes.NetDistribution => BuildNetDistribution(year, currency, organizationUnitName, results),
            CompensationAnalyticsChartTypes.NetByEmployee => BuildNetByEmployee(year, currency, organizationUnitName, results),
            CompensationAnalyticsChartTypes.MonthlyByEmployee => BuildMonthlyByEmployee(year, currency, organizationUnitName, results),
            _ => throw new ArgumentException($"Unsupported chart type: {chartType}", nameof(chartType)),
        };
    }

    private static CompensationAnalyticsResponse BuildShareDistribution(
        short year,
        string currency,
        string? organizationUnitName,
        IReadOnlyList<VariableCompensationResult> results)
    {
        var labels = new[] { "[0%, 10%]", "(10%, 20%]", "(20%, 30%]", "(>30%)" };
        var counts = new int[labels.Length];
        foreach (var result in results)
        {
            var share = result.CompensationPercent;
            if (share <= 0.1m)
            {
                counts[0]++;
            }
            else if (share <= 0.2m)
            {
                counts[1]++;
            }
            else if (share <= 0.3m)
            {
                counts[2]++;
            }
            else
            {
                counts[3]++;
            }
        }

        return new CompensationAnalyticsResponse
        {
            ChartType = CompensationAnalyticsChartTypes.ShareDistribution,
            Year = year,
            Currency = currency,
            OrganizationUnitName = organizationUnitName,
            ValueFormat = "count",
            Buckets = TrimEmptyEdgeBuckets(labels, counts),
        };
    }

    private static CompensationAnalyticsResponse BuildShareByEmployee(
        short year,
        string currency,
        string? organizationUnitName,
        IReadOnlyList<VariableCompensationResult> results) =>
        new()
        {
            ChartType = CompensationAnalyticsChartTypes.ShareByEmployee,
            Year = year,
            Currency = currency,
            OrganizationUnitName = organizationUnitName,
            ValueFormat = "percent",
            Series = results
                .OrderBy(r => r.Employee.LastName)
                .ThenBy(r => r.Employee.FirstName)
                .Select(r => new CompensationAnalyticsSeriesPointResponse
                {
                    Label = r.Employee.FullName,
                    Value = Math.Round(r.CompensationPercent * 100m, 2),
                })
                .ToList(),
        };

    private static CompensationAnalyticsResponse BuildNetDistribution(
        short year,
        string currency,
        string? organizationUnitName,
        IReadOnlyList<VariableCompensationResult> results)
    {
        var labels = new[]
        {
            "[0, 50.000]",
            "(50.000, 100.000]",
            "(100.000, 150.000]",
            "(150.000, 200.000]",
            "(200.000, 250.000]",
            "(250.000, 300.000]",
            "(>300.000)",
        };

        var counts = new int[labels.Length];
        foreach (var result in results)
        {
            var net = result.NetCompensation;
            var index = net switch
            {
                <= 50_000m => 0,
                <= 100_000m => 1,
                <= 150_000m => 2,
                <= 200_000m => 3,
                <= 250_000m => 4,
                <= 300_000m => 5,
                _ => 6,
            };
            counts[index]++;
        }

        return new CompensationAnalyticsResponse
        {
            ChartType = CompensationAnalyticsChartTypes.NetDistribution,
            Year = year,
            Currency = currency,
            OrganizationUnitName = organizationUnitName,
            ValueFormat = "count",
            Buckets = TrimEmptyEdgeBuckets(labels, counts),
        };
    }

    private static IReadOnlyList<CompensationAnalyticsBucketResponse> TrimEmptyEdgeBuckets(
        string[] labels,
        int[] counts)
    {
        var first = Array.FindIndex(counts, count => count > 0);
        if (first < 0)
        {
            return labels
                .Select((label, index) => new CompensationAnalyticsBucketResponse
                {
                    Label = label,
                    Count = counts[index],
                })
                .ToList();
        }

        var last = Array.FindLastIndex(counts, count => count > 0);
        return Enumerable.Range(first, last - first + 1)
            .Select(index => new CompensationAnalyticsBucketResponse
            {
                Label = labels[index],
                Count = counts[index],
            })
            .ToList();
    }

    private static CompensationAnalyticsResponse BuildNetByEmployee(
        short year,
        string currency,
        string? organizationUnitName,
        IReadOnlyList<VariableCompensationResult> results) =>
        new()
        {
            ChartType = CompensationAnalyticsChartTypes.NetByEmployee,
            Year = year,
            Currency = currency,
            OrganizationUnitName = organizationUnitName,
            ValueFormat = "currency",
            Series = results
                .OrderBy(r => r.Employee.LastName)
                .ThenBy(r => r.Employee.FirstName)
                .Select(r => new CompensationAnalyticsSeriesPointResponse
                {
                    Label = r.Employee.FullName,
                    Value = r.NetCompensation,
                })
                .ToList(),
        };

    private static CompensationAnalyticsResponse BuildMonthlyByEmployee(
        short year,
        string currency,
        string? organizationUnitName,
        IReadOnlyList<VariableCompensationResult> results) =>
        new()
        {
            ChartType = CompensationAnalyticsChartTypes.MonthlyByEmployee,
            Year = year,
            Currency = currency,
            OrganizationUnitName = organizationUnitName,
            ValueFormat = "currency",
            Series = results
                .OrderBy(r => r.Employee.LastName)
                .ThenBy(r => r.Employee.FirstName)
                .Select(r => new CompensationAnalyticsSeriesPointResponse
                {
                    Label = r.Employee.FullName,
                    Value = r.MonthlyCompensation,
                })
                .ToList(),
        };
}
