using FluentAssertions;
using VariableCompensation.Application.Evaluation.Commands;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fakes;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Application.Tests.Evaluation.Handlers;

/// <summary>Each goal carries the share of the goals average the evaluator gives it, and the shares make 100%.</summary>
[Trait("Category", "Unit")]
public class GoalWeightsTests
{
    [Fact]
    public async Task WeightsMakingAHundred_AreSaved()
    {
        var repository = SeedDraft();

        var result = await Replace(repository, Goal("Cilj 1", 60), Goal("Cilj 2", 40));

        result.IsSuccess.Should().BeTrue();
        repository.Store[1].Goals.Select(g => g.Weight).Should().Equal(60m, 40m);
    }

    [Fact]
    public async Task GoalsWithoutWeights_CountEqually()
    {
        var repository = SeedDraft();

        var result = await Replace(repository, Goal("Cilj 1", null), Goal("Cilj 2", null));

        result.IsSuccess.Should().BeTrue();
        repository.Store[1].Goals.Should().OnlyContain(g => g.Weight == null);
    }

    [Fact]
    public async Task WeightsOnSomeGoalsOnly_AreRefused()
    {
        var result = await Replace(SeedDraft(), Goal("Cilj 1", 100), Goal("Cilj 2", null));

        result.Error.Should().Be(ErrorCodes.GoalWeightsIncomplete);
    }

    [Theory]
    [InlineData(70, 30.5)]
    [InlineData(97, 3)]
    [InlineData(0, 100)]
    public async Task WeightsThatAreNotWholePercentsFromTheMinimum_AreRefused(decimal first, decimal second)
    {
        var result = await Replace(SeedDraft(), Goal("Cilj 1", first), Goal("Cilj 2", second));

        result.Error.Should().Be($"{ErrorCodes.GoalWeightInvalid}?min={EvaluationDraftMutator.MinGoalWeightPercent}");
    }

    [Fact]
    public async Task WeightsNotMakingAHundred_AreRefused_AndNothingChanges()
    {
        var repository = SeedDraft();

        var result = await Replace(repository, Goal("Cilj 1", 50), Goal("Cilj 2", 40));

        result.Error.Should().Be($"{ErrorCodes.GoalWeightsSumInvalid}?total=90");
        repository.Store[1].Goals.Should().BeEmpty();
    }

    [Fact]
    public void WeightedGoalsAverage_FollowsTheWeights()
    {
        var evaluation = new EvaluationBuilder()
            .AddGoal(RatingLevelsFixture.RatedLevelId(5), weight: 75)
            .AddGoal(RatingLevelsFixture.RatedLevelId(1), weight: 25)
            .AddMeasure(RatingLevelsFixture.RatedLevelId(3))
            .Build();

        new EvaluationScoringService().Recalculate(
            evaluation,
            RatingLevelsFixture.Create(),
            DescriptiveRatingsFixture.Create());

        evaluation.GoalsAverage.Should().Be(4m);
        evaluation.OverallAverage.Should().Be(3.5m);
    }

    private static EvaluationGoalItem Goal(string description, decimal? weight) =>
        new(description, null, null, weight, 1);

    private static FakeEvaluationRepository SeedDraft()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder().WithId(1).WithEvaluator(2).WithVersion(1).Build());
        return repository;
    }

    private static Task<CSharpFunctionalExtensions.Result<VariableCompensation.Application.Evaluation.Models.EvaluationDetailResponse>> Replace(
        FakeEvaluationRepository repository,
        params EvaluationGoalItem[] goals)
    {
        var userService = FakeCurrentUserService.AsEvaluator();
        var handler = new ReplaceEvaluationGoalsCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, new FakeCurrentEmployeeContext { EmployeeId = 2 }));

        return handler.Handle(new ReplaceEvaluationGoalsCommand(1, 1, goals), CancellationToken.None);
    }
}
