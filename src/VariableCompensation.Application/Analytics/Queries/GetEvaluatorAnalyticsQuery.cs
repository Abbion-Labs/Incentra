using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Analytics.Models;
using VariableCompensation.Application.Hr.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Analytics.Queries;

public sealed record GetEvaluatorAnalyticsQuery(short? Year = null, long? EvaluatorEmployeeId = null)
    : IRequest<Result<EvaluatorAnalyticsResponse>>;

public sealed class GetEvaluatorAnalyticsQueryHandler
    : IRequestHandler<GetEvaluatorAnalyticsQuery, Result<EvaluatorAnalyticsResponse>>
{
    private readonly IAnalyticsRepository analyticsRepository;
    private readonly IEmployeeRepository employeeRepository;
    private readonly IDescriptiveRatingRepository descriptiveRatingRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly ICurrentEmployeeContext currentEmployeeContext;
    private readonly ControllerSupervisionService controllerSupervisionService;

    public GetEvaluatorAnalyticsQueryHandler(
        IAnalyticsRepository analyticsRepository,
        IEmployeeRepository employeeRepository,
        IDescriptiveRatingRepository descriptiveRatingRepository,
        ICurrentUserService currentUserService,
        ICurrentEmployeeContext currentEmployeeContext,
        ControllerSupervisionService controllerSupervisionService)
    {
        this.analyticsRepository = analyticsRepository;
        this.employeeRepository = employeeRepository;
        this.descriptiveRatingRepository = descriptiveRatingRepository;
        this.currentUserService = currentUserService;
        this.currentEmployeeContext = currentEmployeeContext;
        this.controllerSupervisionService = controllerSupervisionService;
    }

    public async Task<Result<EvaluatorAnalyticsResponse>> Handle(
        GetEvaluatorAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var currentEmployeeId = await this.currentEmployeeContext.GetEmployeeIdAsync(cancellationToken);
        if (currentEmployeeId is null && !this.currentUserService.IsAdmin)
        {
            return Result.Failure<EvaluatorAnalyticsResponse>(ErrorCodes.UserNotLinkedToEmployee);
        }

        // The session works in one role, and the token carries only that role.
        var sessionRole = this.currentUserService.ActiveRole;

        long? evaluatorEmployeeId = request.EvaluatorEmployeeId;
        if (this.currentUserService.IsAdmin)
        {
            evaluatorEmployeeId ??= currentEmployeeId;
        }
        else if (sessionRole == RoleCodes.Evaluator)
        {
            if (evaluatorEmployeeId is not null && evaluatorEmployeeId != currentEmployeeId)
            {
                return Result.Failure<EvaluatorAnalyticsResponse>(ErrorCodes.EvaluatorOwnAnalyticsOnly);
            }

            evaluatorEmployeeId = currentEmployeeId;
        }
        else if (sessionRole == RoleCodes.Controller)
        {
            if (evaluatorEmployeeId is null)
            {
                return Result.Failure<EvaluatorAnalyticsResponse>(ErrorCodes.EvaluatorIdRequired);
            }

            if (!await this.controllerSupervisionService.SupervisesEvaluatorAsync(
                    currentEmployeeId!.Value,
                    evaluatorEmployeeId.Value,
                    cancellationToken))
            {
                return Result.Failure<EvaluatorAnalyticsResponse>(ErrorCodes.ControllerDoesNotSuperviseEvaluator);
            }
        }
        else
        {
            return Result.Failure<EvaluatorAnalyticsResponse>(ErrorCodes.EvaluatorAnalyticsAccessDenied);
        }

        if (evaluatorEmployeeId is null)
        {
            return Result.Failure<EvaluatorAnalyticsResponse>(ErrorCodes.EvaluatorIdRequired);
        }

        var evaluator = await this.employeeRepository.FindByIdAsync(evaluatorEmployeeId.Value, cancellationToken);
        if (evaluator is null)
        {
            return Result.Failure<EvaluatorAnalyticsResponse>(ErrorCodes.EvaluatorNotFound);
        }

        var year = request.Year ?? (short)DateTime.UtcNow.Year;
        var previousYear = (short)(year - 1);

        var subordinateCount = await this.analyticsRepository.GetActiveSubordinateCountAsync(
            evaluatorEmployeeId.Value,
            cancellationToken);

        var thisYearCounts = await this.analyticsRepository.GetDescriptiveRatingCountsByEvaluatorAsync(
            evaluatorEmployeeId.Value,
            year,
            cancellationToken);
        var previousYearCounts = await this.analyticsRepository.GetDescriptiveRatingCountsByEvaluatorAsync(
            evaluatorEmployeeId.Value,
            previousYear,
            cancellationToken);

        var ratedThisYear = thisYearCounts.Sum(x => x.Count);
        var recommendedShareByCode = await this.descriptiveRatingRepository.GetRecommendedShareByCodeAsync(cancellationToken);
        var recommendedCounts = BuildRecommendedCounts(ratedThisYear, thisYearCounts, recommendedShareByCode);

        var distribution = thisYearCounts
            .Select(row => new DescriptiveRatingDistributionItem
            {
                DescriptiveRatingId = row.DescriptiveRatingId,
                Code = row.Code,
                Name = row.Name,
                Count = row.Count,
                Percentage = ratedThisYear > 0
                    ? Math.Round((decimal)row.Count / ratedThisYear * 100m, 1)
                    : 0m,
                SortOrder = row.SortOrder,
            })
            .ToList();

        var previousLookup = previousYearCounts.ToDictionary(x => x.DescriptiveRatingId);
        var comparison = thisYearCounts
            .Select(row => new DescriptiveRatingComparisonItem
            {
                DescriptiveRatingId = row.DescriptiveRatingId,
                Code = row.Code,
                Name = row.Name,
                PreviousYearCount = previousLookup.GetValueOrDefault(row.DescriptiveRatingId)?.Count ?? 0,
                ThisYearCount = row.Count,
                RecommendedCount = recommendedCounts.GetValueOrDefault(row.DescriptiveRatingId),
                SortOrder = row.SortOrder,
            })
            .ToList();

        var evaluatorAveragesSelectedYear = await this.analyticsRepository.GetOverallAveragesByEvaluatorAsync(
            evaluatorEmployeeId.Value,
            year,
            cancellationToken);
        var evaluatorAveragesAllYears = await this.analyticsRepository.GetOverallAveragesByEvaluatorAsync(
            evaluatorEmployeeId.Value,
            null,
            cancellationToken);

        return new EvaluatorAnalyticsResponse
        {
            EvaluatorEmployeeId = evaluatorEmployeeId.Value,
            EvaluatorFullName = evaluator.FullName,
            Year = year,
            PreviousYear = previousYear,
            SubordinateCount = subordinateCount,
            RatedEvaluationsThisYear = ratedThisYear,
            DistributionThisYear = distribution,
            RatingComparison = comparison,
            OverallStats = new OverallStatsComparison
            {
                SelectedYear = BuildPeriodStats(evaluatorAveragesSelectedYear),
                AllYears = BuildPeriodStats(evaluatorAveragesAllYears),
            },
        };
    }

    private static EvaluatorPeriodStats BuildPeriodStats(IReadOnlyList<decimal> values) =>
        new()
        {
            Average = Average(values),
            Median = Median(values),
            Variance = Variance(values),
        };

    /// <summary>
    /// How the evaluations rated this year would split over the descriptive ratings if they followed the
    /// recommended shares, so the chart sets like against like: evaluations against evaluations, whatever the number
    /// of quarters behind. The shares are scaled to add up to 100%, as an administrator may leave them off while
    /// adjusting them one by one.
    /// </summary>
    internal static Dictionary<long, int> BuildRecommendedCounts(
        int ratedCount,
        IReadOnlyList<DescriptiveRatingCountRow> ratings,
        IReadOnlyDictionary<string, decimal> recommendedShareByCode)
    {
        var shareTotal = ratings.Sum(r => recommendedShareByCode.GetValueOrDefault(r.Code, 0m));
        if (ratedCount <= 0 || ratings.Count == 0 || shareTotal <= 0m)
        {
            return ratings.ToDictionary(r => r.DescriptiveRatingId, _ => 0);
        }

        var raw = ratings
            .Select(r => new
            {
                r.DescriptiveRatingId,
                Raw = ratedCount * recommendedShareByCode.GetValueOrDefault(r.Code, 0m) / shareTotal,
            })
            .ToList();

        var floored = raw
            .Select(x => new { x.DescriptiveRatingId, Count = (int)Math.Floor(x.Raw) })
            .ToList();

        var remainder = ratedCount - floored.Sum(x => x.Count);
        var ranked = raw
            .Select((x, index) => new
            {
                x.DescriptiveRatingId,
                Fraction = x.Raw - Math.Floor(x.Raw),
                index,
            })
            .OrderByDescending(x => x.Fraction)
            .ThenBy(x => x.index)
            .ToList();

        var result = floored.ToDictionary(x => x.DescriptiveRatingId, x => x.Count);
        for (var i = 0; i < remainder && i < ranked.Count; i++)
        {
            result[ranked[i].DescriptiveRatingId]++;
        }

        return result;
    }

    private static decimal? Average(IReadOnlyList<decimal> values) =>
        values.Count == 0 ? null : Math.Round(values.Average(), 2);

    private static decimal? Median(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        var median = sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2m
            : sorted[mid];

        return Math.Round(median, 2);
    }

    private static decimal? Variance(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var avg = values.Average();
        var variance = values.Average(v => (v - avg) * (v - avg));
        return Math.Round(variance, 3);
    }
}
