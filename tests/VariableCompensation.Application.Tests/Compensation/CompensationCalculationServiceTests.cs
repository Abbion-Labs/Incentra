using FluentAssertions;
using VariableCompensation.Application.Compensation.Services;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Builders;
using EvaluationEntity = VariableCompensation.Domain.Entities.Evaluation.Evaluation;

namespace VariableCompensation.Application.Tests.Compensation;

[Trait("Category", "Unit")]
public class CompensationCalculationServiceTests
{
    private readonly CompensationCalculationService service = new();
    private readonly VariableCompensationParameters parameters = new()
    {
        AcceptablePerformanceRating = 2.5m,
        Exponent = 1.5m,
        MonetaryPool = 100_000m,
        DependencyWeight = 1.0m,
        AllowNegativeVariable = false,
    };

    [Theory]
    [InlineData(3.5, 1.0)]
    [InlineData(2.5, 0)]
    [InlineData(2.0, 0)]
    public void CalculatePonder_AboveThreshold_ReturnsExpected(decimal overallAverage, decimal expected)
    {
        var ponder = CompensationCalculationService.CalculatePonder(overallAverage, this.parameters);
        ponder.Should().BeApproximately(expected, 0.0001m);
    }

    [Fact]
    public void CalculatePonder_AllowNegativeVariable_BelowThreshold_ReturnsNegative()
    {
        this.parameters.AllowNegativeVariable = true;
        var ponder = CompensationCalculationService.CalculatePonder(2.0m, this.parameters);
        ponder.Should().BeLessThan(0);
    }

    [Fact]
    public void BuildEmployeeInput_NoEvaluations_ReturnsSkipReason()
    {
        var employee = new EmployeeBuilder().Build();

        var (_, skipReason) = this.service.BuildEmployeeInput(
            employee,
            [],
            this.parameters,
            requireAllQuarters: false,
            points: 20,
            salaryPerPoint: 1000m);

        skipReason.Should().Contain("no approved evaluations");
    }

    [Fact]
    public void BuildEmployeeInput_RequireAllQuartersWithLessThanFour_ReturnsSkipReason()
    {
        var employee = new EmployeeBuilder().Build();
        var evaluations = CreateApprovedEvaluations(2);

        var (_, skipReason) = this.service.BuildEmployeeInput(
            employee,
            evaluations,
            this.parameters,
            requireAllQuarters: true,
            points: 20,
            salaryPerPoint: 1000m);

        skipReason.Should().Contain("missing valid quarterly evaluations");
    }

    [Fact]
    public void DistributePool_TwoEmployees_DistributesFullMonetaryPool()
    {
        var employeeA = new EmployeeBuilder().WithId(1).Build();
        var employeeB = new EmployeeBuilder().WithId(2).WithName("Ana", "Anić").Build();
        var evaluations = CreateApprovedEvaluations(4, overallAverage: 3.0m);

        var inputA = this.service.BuildEmployeeInput(employeeA, evaluations, this.parameters, false, 20, 1000m).Input!;
        var inputB = this.service.BuildEmployeeInput(employeeB, evaluations, this.parameters, false, 30, 1000m).Input!;

        var outputs = this.service.DistributePool([inputA, inputB], this.parameters);

        outputs.Should().HaveCount(2);
        outputs.Sum(o => o.NetCompensation).Should().BeApproximately(this.parameters.MonetaryPool, 0.05m);
        outputs.Should().OnlyContain(o => o.NetCompensation >= 0);
    }

    [Fact]
    public void BuildEmployeeInput_ExcludedEvaluations_AreSkippedFromAverage()
    {
        var employee = new EmployeeBuilder().Build();
        var evaluations = new List<EvaluationEntity>
        {
            new()
            {
                Year = 2026,
                Quarter = 1,
                Status = EvaluationStatus.Approved,
                GoalsAverage = 4.0m,
                MeasuresAverage = 4.0m,
                OverallAverage = 4.0m,
                ExcludedFromCompensation = false,
            },
            new()
            {
                Year = 2026,
                Quarter = 2,
                Status = EvaluationStatus.Approved,
                GoalsAverage = 2.0m,
                MeasuresAverage = 2.0m,
                OverallAverage = 2.0m,
                ExcludedFromCompensation = true,
            },
            new()
            {
                Year = 2026,
                Quarter = 3,
                Status = EvaluationStatus.Approved,
                GoalsAverage = 3.0m,
                MeasuresAverage = 3.0m,
                OverallAverage = 3.0m,
                ExcludedFromCompensation = false,
            },
        };

        var (input, skipReason) = this.service.BuildEmployeeInput(
            employee,
            evaluations,
            this.parameters,
            requireAllQuarters: false,
            points: 20,
            salaryPerPoint: 1000m);

        skipReason.Should().BeNull();
        input!.OverallAverage.Should().BeApproximately(3.5m, 0.0001m);
        input.Evaluations.Should().HaveCount(2);
    }

    [Fact]
    public void BuildEmployeeInput_AllExcluded_ReturnsSkipReason()
    {
        var employee = new EmployeeBuilder().Build();
        var evaluations = CreateApprovedEvaluations(2, overallAverage: 3.0m)
            .Select(e =>
            {
                e.ExcludedFromCompensation = true;
                return e;
            })
            .ToList();

        var (_, skipReason) = this.service.BuildEmployeeInput(
            employee,
            evaluations,
            this.parameters,
            requireAllQuarters: false,
            points: 20,
            salaryPerPoint: 1000m);

        skipReason.Should().Contain("no evaluations that count toward variable compensation");
    }

    private static List<EvaluationEntity> CreateApprovedEvaluations(int count, decimal overallAverage = 3.0m)
    {
        return Enumerable.Range(1, count)
            .Select(quarter => new EvaluationEntity
            {
                Year = 2026,
                Quarter = (byte)quarter,
                Status = EvaluationStatus.Approved,
                GoalsAverage = overallAverage,
                MeasuresAverage = overallAverage,
                OverallAverage = overallAverage,
            })
            .ToList();
    }
}
