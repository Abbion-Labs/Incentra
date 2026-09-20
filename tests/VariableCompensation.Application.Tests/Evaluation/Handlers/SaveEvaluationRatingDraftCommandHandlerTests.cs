using FluentAssertions;
using VariableCompensation.Application.Evaluation.Commands;
using VariableCompensation.Application.Evaluation.Models;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fakes;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Application.Tests.Evaluation.Handlers;

[Trait("Category", "Unit")]
public class SaveEvaluationRatingDraftCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRatingDraft_SavesGoalsMeasuresAndIncrementsVersionOnce()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEvaluator(2)
            .WithVersion(1)
            .AddGoal(RatingLevelsFixture.NotRatedId, description: "Cilj 1")
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var handler = new SaveEvaluationRatingDraftCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext));

        var ratedGoalLevel = RatingLevelsFixture.RatedLevelId(4);
        var ratedMeasureLevel = RatingLevelsFixture.RatedLevelId(3);

        var result = await handler.Handle(new SaveEvaluationRatingDraftCommand(
            1,
            1,
            DateTime.UtcNow,
            "Ocena komentar",
            null,
            true,
            [
                new EvaluationGoalItem("Cilj 1", ratedGoalLevel, "Komentar cilja", 50m, 1),
            ],
            [
                new EvaluationMeasureItem(1, null, ratedMeasureLevel, 1),
            ],
            new EvaluationTrainingDraftItem("Obuka", "Znanje", "Razvoj", "Trening komentar")), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = repository.Store[1];
        stored.Version.Should().Be(2);
        stored.EvaluatorComment.Should().Be("Ocena komentar");
        stored.Goals.Single().RatingLevelId.Should().Be(ratedGoalLevel);
        stored.Measures.Should().ContainSingle(m => m.RatingLevelId == ratedMeasureLevel);
        stored.Training.Should().NotBeNull();
        stored.Training!.TrainingDescription.Should().Be("Obuka");
        stored.OverallAverage.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ConditionsNotMet_UpdatesHeaderWithoutMeasures()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEvaluator(2)
            .WithVersion(1)
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .AddMeasure(RatingLevelsFixture.RatedLevelId(3))
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var handler = new SaveEvaluationRatingDraftCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext));

        var previousGoalRating = repository.Store[1].Goals.Single().RatingLevelId;
        var previousMeasureRating = repository.Store[1].Measures.Single().RatingLevelId;

        var result = await handler.Handle(new SaveEvaluationRatingDraftCommand(
            1,
            1,
            null,
            null,
            "Uslovi nisu ispunjeni",
            false,
            null,
            null,
            null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = repository.Store[1];
        stored.Version.Should().Be(2);
        stored.ConditionsFulfilled.Should().BeFalse();
        stored.ConditionsNotMetComment.Should().Be("Uslovi nisu ispunjeni");
        stored.Goals.Single().RatingLevelId.Should().Be(previousGoalRating);
        stored.Measures.Single().RatingLevelId.Should().Be(previousMeasureRating);
    }
}
