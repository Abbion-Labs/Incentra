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
public class SaveEvaluationPlanningDraftCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidPlanningDraft_SavesAllSectionsAndIncrementsVersionOnce()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEvaluator(2)
            .WithVersion(1)
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var handler = new SaveEvaluationPlanningDraftCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext));

        var result = await handler.Handle(new SaveEvaluationPlanningDraftCommand(
            1,
            1,
            DateTime.UtcNow,
            "Plan komentar",
            "Uslovi nisu ispunjeni",
            false,
            [
                new EvaluationGoalItem("Cilj 1", null, null, null, 1),
            ],
            [
                new EvaluationConditionItem("Uslov 1", 1),
            ],
            [
                new EvaluationCriterionItem("Kriterijum 1", 1),
            ]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = repository.Store[1];
        stored.Version.Should().Be(2);
        stored.EvaluatorComment.Should().Be("Plan komentar");
        stored.ConditionsNotMetComment.Should().Be("Uslovi nisu ispunjeni");
        stored.ConditionsFulfilled.Should().BeFalse();
        stored.Goals.Should().ContainSingle(g => g.Description == "Cilj 1");
        stored.Conditions.Should().ContainSingle(c => c.Description == "Uslov 1");
        stored.Criteria.Should().ContainSingle(c => c.Description == "Kriterijum 1");
    }

    [Fact]
    public async Task Handle_VersionConflict_ReturnsError()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEvaluator(2)
            .WithVersion(2)
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var handler = new SaveEvaluationPlanningDraftCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext));

        var result = await handler.Handle(new SaveEvaluationPlanningDraftCommand(
            1,
            1,
            null,
            null,
            null,
            true,
            [],
            [],
            []), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        repository.Store[1].Version.Should().Be(2);
    }
}
