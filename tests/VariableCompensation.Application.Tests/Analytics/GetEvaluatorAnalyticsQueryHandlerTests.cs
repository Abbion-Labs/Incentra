using FluentAssertions;
using NSubstitute;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Analytics.Queries;
using VariableCompensation.Application.Hr.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Analytics;

[Trait("Category", "Unit")]
public class GetEvaluatorAnalyticsQueryHandlerTests
{
    private const long CurrentEmployeeId = 3;
    private const long SupervisedEvaluatorId = 2;

    private readonly IEmployeeRepository employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository = Substitute.For<IEvaluatorSettingsRepository>();
    private readonly FakeCurrentUserService userService = new() { Roles = [RoleCodes.Controller] };
    private readonly GetEvaluatorAnalyticsQueryHandler handler;

    public GetEvaluatorAnalyticsQueryHandlerTests()
    {
        this.evaluatorSettingsRepository.FindByEmployeeIdAsync(SupervisedEvaluatorId, Arg.Any<CancellationToken>())
            .Returns(new EvaluatorSettings { EmployeeId = SupervisedEvaluatorId, ControllerEmployeeId = CurrentEmployeeId });

        this.handler = new GetEvaluatorAnalyticsQueryHandler(
            Substitute.For<IAnalyticsRepository>(),
            this.employeeRepository,
            Substitute.For<IDescriptiveRatingRepository>(),
            this.userService,
            new FakeCurrentEmployeeContext { EmployeeId = CurrentEmployeeId },
            new ControllerSupervisionService(this.evaluatorSettingsRepository, this.employeeRepository));
    }

    [Fact]
    public async Task Handle_EvaluatorSession_CannotOpenAnotherEvaluator()
    {
        this.userService.Roles = [RoleCodes.Evaluator];

        var result = await this.handler.Handle(
            new GetEvaluatorAnalyticsQuery(2026, SupervisedEvaluatorId),
            CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.EvaluatorOwnAnalyticsOnly);
    }

    [Fact]
    public async Task Handle_ControllerSession_OpensASupervisedEvaluator()
    {
        this.userService.Roles = [RoleCodes.Controller];

        var result = await this.handler.Handle(
            new GetEvaluatorAnalyticsQuery(2026, SupervisedEvaluatorId),
            CancellationToken.None);

        // The access check passed: the handler went on to load the evaluator,
        // whom this test does not set up.
        result.Error.Should().Be(ErrorCodes.EvaluatorNotFound);
        await this.employeeRepository.Received(1).FindByIdAsync(SupervisedEvaluatorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ControllerSession_CannotOpenAnUnsupervisedEvaluator()
    {
        this.userService.Roles = [RoleCodes.Controller];

        var result = await this.handler.Handle(
            new GetEvaluatorAnalyticsQuery(2026, 99),
            CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.ControllerDoesNotSuperviseEvaluator);
    }

    [Fact]
    public async Task Handle_AdminSession_OpensAnyEvaluator()
    {
        this.userService.Roles = [RoleCodes.Admin];

        var result = await this.handler.Handle(
            new GetEvaluatorAnalyticsQuery(2026, 99),
            CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.EvaluatorNotFound);
    }

    [Fact]
    public void RecommendedCounts_SplitTheRatedEvaluations()
    {
        // Twenty people rated in all four quarters: 80 evaluations, so 45% of them is 36, not 45% of 20 people.
        var counts = GetEvaluatorAnalyticsQueryHandler.BuildRecommendedCounts(
            80,
            Ratings("MEETS", "GOOD", "EXCEEDS"),
            new Dictionary<string, decimal> { ["MEETS"] = 0.25m, ["GOOD"] = 0.45m, ["EXCEEDS"] = 0.30m });

        counts.Should().Equal(new Dictionary<long, int> { [1] = 20, [2] = 36, [3] = 24 });
    }

    [Fact]
    public void RecommendedCounts_ScaleSharesThatDoNotAddUpToAHundredPercent()
    {
        // 60% + 60%: read as half and half.
        var counts = GetEvaluatorAnalyticsQueryHandler.BuildRecommendedCounts(
            10,
            Ratings("GOOD", "EXCEEDS"),
            new Dictionary<string, decimal> { ["GOOD"] = 0.60m, ["EXCEEDS"] = 0.60m });

        counts.Should().Equal(new Dictionary<long, int> { [1] = 5, [2] = 5 });
    }

    [Fact]
    public void RecommendedCounts_AreZeroWithoutRatedEvaluations()
    {
        var counts = GetEvaluatorAnalyticsQueryHandler.BuildRecommendedCounts(
            0,
            Ratings("GOOD"),
            new Dictionary<string, decimal> { ["GOOD"] = 1m });

        counts.Should().Equal(new Dictionary<long, int> { [1] = 0 });
    }

    private static IReadOnlyList<DescriptiveRatingCountRow> Ratings(params string[] codes) =>
        codes.Select((code, i) => new DescriptiveRatingCountRow(i + 1, code, code, i + 1, 0)).ToList();
}
