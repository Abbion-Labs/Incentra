using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VariableCompensation.Application.Evaluation.Commands;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fakes;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Application.Tests.Evaluation.Handlers;

/// <summary>A plan is goals, conditions and criteria together, and an evaluation is rated against a whole one.</summary>
[Trait("Category", "Unit")]
public class PlanCompletenessTests
{
    [Fact]
    public async Task SubmittingWithoutConditionsAndCriteria_IsRefused()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEvaluator(2)
            .WithController(3)
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .AddMeasure(RatingLevelsFixture.RatedLevelId(4))
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var handler = new SubmitEvaluationCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, new FakeCurrentEmployeeContext { EmployeeId = 2 }),
            new FakeEvaluationNotificationService(),
            NullLogger<SubmitEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new SubmitEvaluationCommand(1, 1), CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.PlanIncomplete);
        repository.Store[1].Status.Should().Be(EvaluationStatus.Draft);
    }

    [Fact]
    public async Task SavingAPlanWithoutCriteria_IsRefused_AndNothingChanges()
    {
        var repository = SeedDraft();

        var result = await SavePlan(repository, [Goal()], [new EvaluationConditionItem("Uslov", 1)], []);

        result.Error.Should().Be(ErrorCodes.PlanIncomplete);
        repository.Store[1].Goals.Should().BeEmpty();
        repository.Store[1].Version.Should().Be(1);
    }

    [Fact]
    public async Task CompletingAPlanWhoseConditionsAndCriteriaWereSetEarlier_SavesIt()
    {
        var repository = SeedDraft();
        repository.Store[1].Conditions.Add(new EvaluationCondition { Description = "Uslov", SortOrder = 1 });
        repository.Store[1].Criteria.Add(new EvaluationCriterion { Description = "Kriterijum", SortOrder = 1 });

        var result = await SavePlan(
            repository,
            [Goal()],
            [new EvaluationConditionItem("Uslov", 1)],
            [new EvaluationCriterionItem("Kriterijum", 1)]);

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error : null);
        EvaluationPlanningRules.IsGoalsPlanningComplete(repository.Store[1]).Should().BeTrue();
    }

    private static EvaluationGoalItem Goal() => new("Cilj", null, null, null, 1);

    private static FakeEvaluationRepository SeedDraft()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder().WithId(1).WithEvaluator(2).WithVersion(1).Build());
        return repository;
    }

    private static Task<CSharpFunctionalExtensions.Result<VariableCompensation.Application.Evaluation.Models.EvaluationDetailResponse>> SavePlan(
        FakeEvaluationRepository repository,
        IReadOnlyList<EvaluationGoalItem> goals,
        IReadOnlyList<EvaluationConditionItem> conditions,
        IReadOnlyList<EvaluationCriterionItem> criteria)
    {
        var userService = FakeCurrentUserService.AsEvaluator();
        var handler = new SaveEvaluationPlanningDraftCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, new FakeCurrentEmployeeContext { EmployeeId = 2 }));

        return handler.Handle(
            new SaveEvaluationPlanningDraftCommand(1, 1, DateTime.UtcNow, null, null, true, goals, conditions, criteria),
            CancellationToken.None);
    }
}
