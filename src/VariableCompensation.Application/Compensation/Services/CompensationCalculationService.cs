using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Compensation.Services;

public sealed class CompensationCalculationService
{
    public const string FormulaVersion = "v2-diploma";

    public sealed class EmployeeCalculationInput
    {
        public required Employee Employee { get; init; }

        public required IReadOnlyList<EvaluationEntity> Evaluations { get; init; }

        public decimal GoalsAverage { get; init; }

        public decimal MeasuresAverage { get; init; }

        public decimal OverallAverage { get; init; }

        public int Points { get; init; }

        public decimal SalaryPerPoint { get; init; }

        public decimal Ponder { get; init; }
    }

    public sealed class EmployeeCalculationOutput
    {
        public required EmployeeCalculationInput Input { get; init; }

        public decimal ZScore { get; init; }

        public decimal NormalizedZScore { get; init; }

        public decimal NormalizedPoints { get; init; }

        public decimal CompensationWithoutSalary { get; init; }

        public decimal Aq { get; init; }

        public decimal NetCompensation { get; init; }

        public decimal QuarterlyCompensation { get; init; }

        public decimal MonthlyCompensation { get; init; }

        public decimal CompensationPercent { get; init; }
    }

    public (EmployeeCalculationInput? Input, string? SkipReason) BuildEmployeeInput(
        Employee employee,
        IReadOnlyList<EvaluationEntity> evaluations,
        VariableCompensationParameters parameters,
        bool requireAllQuarters,
        int points,
        decimal salaryPerPoint,
        bool? allowNegativeVariableOverride = null)
    {
        if (evaluations.Count == 0)
        {
            return (null, $"Employee {employee.FullName} has no approved evaluations.");
        }

        var eligible = evaluations.Where(e => !e.ExcludedFromCompensation).ToList();
        if (eligible.Count == 0)
        {
            return (null, $"Employee {employee.FullName} has no evaluations that count toward variable compensation.");
        }

        if (requireAllQuarters && eligible.Count < 4)
        {
            return (null, $"Employee {employee.FullName} is missing valid quarterly evaluations for compensation ({eligible.Count}/4).");
        }

        if (points <= 0)
        {
            return (null, $"Employee {employee.FullName} has no configured points.");
        }

        if (salaryPerPoint <= 0)
        {
            return (null, $"Employee {employee.FullName} has no configured salary per point.");
        }

        var goalsAverage = eligible.Average(e => e.GoalsAverage!.Value);
        var measuresAverage = eligible.Average(e => e.MeasuresAverage!.Value);
        var overallAverage = eligible.Average(e => e.OverallAverage!.Value);
        var ponder = CalculatePonder(overallAverage, parameters, allowNegativeVariableOverride);

        return (new EmployeeCalculationInput
        {
            Employee = employee,
            Evaluations = eligible,
            GoalsAverage = Math.Round(goalsAverage, 4),
            MeasuresAverage = Math.Round(measuresAverage, 4),
            OverallAverage = Math.Round(overallAverage, 4),
            Points = points,
            SalaryPerPoint = salaryPerPoint,
            Ponder = Math.Round(ponder, 6)
        }, null);
    }

    public static decimal CalculatePonder(
        decimal overallAverage,
        VariableCompensationParameters parameters,
        bool? allowNegativeVariableOverride = null)
    {
        var allowNegativeVariable = allowNegativeVariableOverride ?? parameters.AllowNegativeVariable;
        var difference = overallAverage - parameters.AcceptablePerformanceRating;

        if (overallAverage > parameters.AcceptablePerformanceRating)
        {
            return (decimal)Math.Pow((double)difference, (double)parameters.Exponent);
        }

        if (!allowNegativeVariable)
        {
            return 0m;
        }

        if (difference < 0)
        {
            return -(decimal)Math.Pow((double)Math.Abs(difference), (double)parameters.Exponent);
        }

        return difference == 0
            ? 0m
            : (decimal)Math.Pow((double)difference, (double)parameters.Exponent);
    }

    public IReadOnlyList<EmployeeCalculationOutput> DistributePool(
        IReadOnlyList<EmployeeCalculationInput> inputs,
        VariableCompensationParameters parameters)
    {
        if (inputs.Count == 0)
        {
            return Array.Empty<EmployeeCalculationOutput>();
        }

        var pointsList = inputs.Select(i => i.Points).ToList();
        var meanPoints = (decimal)pointsList.Average();
        var variance = CalculateVariance(pointsList, meanPoints);
        var maxPoints = pointsList.Max();
        var maxPointsSafe = maxPoints > 0 ? maxPoints : 1;

        var ponders = inputs.Select(i => i.Ponder).ToList();
        var ponderSum = ponders.Sum();

        var intermediate = inputs.Select((input, index) =>
        {
            var zScore = variance > 0 ? (input.Points - meanPoints) / variance : 0m;
            return new
            {
                Input = input,
                ZScore = Math.Round(zScore, 6),
                NormalizedPoints = Math.Round((decimal)input.Points / maxPointsSafe, 6),
                CompensationWithoutSalary = ponderSum != 0
                    ? Math.Round(parameters.MonetaryPool * input.Ponder / ponderSum, 2)
                    : 0m
            };
        }).ToList();

        var maxZScore = intermediate.Max(x => x.ZScore);
        var maxZScoreSafe = maxZScore != 0 ? maxZScore : 1m;

        var withAq = intermediate.Select(item =>
        {
            var normalizedZ = item.ZScore / maxZScoreSafe;
            var aq = item.Input.Ponder * (1m + normalizedZ * parameters.DependencyWeight);
            return new
            {
                item.Input,
                item.ZScore,
                NormalizedZScore = Math.Round(normalizedZ, 6),
                item.NormalizedPoints,
                item.CompensationWithoutSalary,
                Aq = Math.Round(aq, 6)
            };
        }).ToList();

        var aqSum = withAq.Sum(x => x.Aq);

        return withAq.Select(item =>
        {
            var net = aqSum != 0
                ? Math.Round(item.Aq / aqSum * parameters.MonetaryPool, 2)
                : 0m;
            var monthly = Math.Round(net / 12m, 2);
            var fixedMonthlySalary = item.Input.Points * item.Input.SalaryPerPoint;
            var compensationPercent = fixedMonthlySalary > 0
                ? Math.Round(monthly / fixedMonthlySalary, 6)
                : 0m;

            return new EmployeeCalculationOutput
            {
                Input = item.Input,
                ZScore = item.ZScore,
                NormalizedZScore = item.NormalizedZScore,
                NormalizedPoints = item.NormalizedPoints,
                CompensationWithoutSalary = item.CompensationWithoutSalary,
                Aq = item.Aq,
                NetCompensation = net,
                QuarterlyCompensation = Math.Round(net / 4m, 2),
                MonthlyCompensation = monthly,
                CompensationPercent = compensationPercent
            };
        }).ToList();
    }

    private static decimal CalculateVariance(IReadOnlyList<int> values, decimal mean)
    {
        if (values.Count == 0)
        {
            return 0m;
        }

        return values.Sum(v => (v - mean) * (v - mean)) / values.Count;
    }
}
