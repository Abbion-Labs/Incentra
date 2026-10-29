using FluentAssertions;
using VariableCompensation.Application.Evaluation.Commands;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fakes;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Application.Tests.Evaluation.Handlers;

/// <summary>Once goals, conditions and criteria are set, rating may only rate the goals that were agreed.</summary>
[Trait("Category", "Unit")]
public class AgreedGoalsLockTests
{
    private static readonly long Rated = RatingLevelsFixture.RatedLevelId(4);

    [Fact]
    public async Task RatingTheAgreedGoals_ChangesOnlyTheirRatingAndComment()
    {
        var repository = SeedAgreedPlan();

        var result = await Save(repository, [new EvaluationGoalItem("Cilj 1", Rated, "Dobro urađeno", null, 1, Id: 1)]);

        result.IsSuccess.Should().BeTrue();
        var goal = repository.Store[1].Goals.Single();
        goal.Id.Should().Be(1);
        goal.Description.Should().Be("Cilj 1");
        goal.RatingLevelId.Should().Be(Rated);
        goal.Comment.Should().Be("Dobro urađeno");
    }

    [Fact]
    public async Task RewordingAnAgreedGoal_IsRefused()
    {
        var repository = SeedAgreedPlan();

        var result = await Save(repository, [new EvaluationGoalItem("Lakši cilj", Rated, null, null, 1, Id: 1)]);

        result.Error.Should().Be(ErrorCodes.GoalsPlanningLocked);
        repository.Store[1].Goals.Single().Description.Should().Be("Cilj 1");
    }

    [Fact]
    public async Task ChangingTheWeightOfAnAgreedGoal_IsRefused()
    {
        var repository = SeedAgreedPlan();

        var result = await Save(repository, [new EvaluationGoalItem("Cilj 1", Rated, null, 5m, 1, Id: 1)]);

        result.Error.Should().Be(ErrorCodes.GoalsPlanningLocked);
    }

    [Fact]
    public async Task AddingAGoal_IsRefused()
    {
        var repository = SeedAgreedPlan();

        var result = await Save(repository,
        [
            new EvaluationGoalItem("Cilj 1", Rated, null, null, 1, Id: 1),
            new EvaluationGoalItem("Novi cilj", Rated, null, null, 2),
        ]);

        result.Error.Should().Be(ErrorCodes.GoalsPlanningLocked);
        repository.Store[1].Goals.Should().ContainSingle();
    }

    [Fact]
    public async Task AnEmptyGoalList_IsRefused_AndKeepsTheGoals()
    {
        var repository = SeedAgreedPlan();

        var result = await Save(repository, []);

        result.Error.Should().Be(ErrorCodes.GoalsPlanningLocked);
        repository.Store[1].Goals.Should().ContainSingle();
    }

    [Fact]
    public async Task LeavingTheGoalsOut_KeepsThemAsTheyAre()
    {
        var repository = SeedAgreedPlan();

        var result = await Save(repository, null);

        result.IsSuccess.Should().BeTrue();
        repository.Store[1].Goals.Single().Description.Should().Be("Cilj 1");
    }

    [Fact]
    public async Task ReplacingTheGoalsDirectly_IsRefusedToo()
    {
        var repository = SeedAgreedPlan();
        var (userService, access) = Evaluator();
        var handler = new ReplaceEvaluationGoalsCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            access);

        var result = await handler.Handle(
            new ReplaceEvaluationGoalsCommand(1, 1, [new EvaluationGoalItem("Drugi cilj", null, null, null, 1)]),
            CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.GoalsPlanningLocked);
        repository.Store[1].Goals.Single().Description.Should().Be("Cilj 1");
    }

    private static FakeEvaluationRepository SeedAgreedPlan()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEvaluator(2)
            .WithVersion(1)
            .AddGoal(RatingLevelsFixture.NotRatedId, description: "Cilj 1")
            .WithAgreedPlan()
            .Build());
        return repository;
    }

    private static (FakeCurrentUserService UserService, EvaluationAccessService Access) Evaluator()
    {
        var userService = FakeCurrentUserService.AsEvaluator();
        return (userService, new EvaluationAccessService(userService, new FakeCurrentEmployeeContext { EmployeeId = 2 }));
    }

    private static Task<CSharpFunctionalExtensions.Result<VariableCompensation.Application.Evaluation.Models.EvaluationDetailResponse>> Save(
        FakeEvaluationRepository repository,
        IReadOnlyList<EvaluationGoalItem>? goals)
    {
        var (userService, access) = Evaluator();
        var handler = new SaveEvaluationRatingDraftCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            access);

        return handler.Handle(
            new SaveEvaluationRatingDraftCommand(1, 1, null, null, null, true, goals, null, null),
            CancellationToken.None);
    }
}
