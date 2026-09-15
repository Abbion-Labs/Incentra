using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VariableCompensation.Application.Evaluation.Commands;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fakes;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Application.Tests.Evaluation.Handlers;

[Trait("Category", "Unit")]
public class ApproveEvaluationCommandHandlerTests
{
    [Fact]
    public async Task Handle_UnderReview_ApprovesEvaluation()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithController(3)
            .WithStatus(EvaluationStatus.UnderReview)
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .AddMeasure(RatingLevelsFixture.RatedLevelId(3))
            .Build());

        var userService = FakeCurrentUserService.AsController();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 3 };
        var handler = new ApproveEvaluationCommandHandler(
            repository,
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<ApproveEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new ApproveEvaluationCommand(1, 1, "Odobreno"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(EvaluationStatus.Approved));
    }

    [Fact]
    public async Task Handle_ConditionsNotMet_SetsExcludedFromCompensation()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithController(3)
            .WithStatus(EvaluationStatus.UnderReview)
            .WithConditionsFulfilled(false)
            .WithConditionsNotMetComment("Uslovi nisu ispunjeni")
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .Build());

        var userService = FakeCurrentUserService.AsController();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 3 };
        var handler = new ApproveEvaluationCommandHandler(
            repository,
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<ApproveEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new ApproveEvaluationCommand(1, 1, "Odobreno"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Store[1].ExcludedFromCompensation.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ConditionsFulfilled_DoesNotExcludeFromCompensation()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithController(3)
            .WithStatus(EvaluationStatus.UnderReview)
            .WithConditionsFulfilled(true)
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .AddMeasure(RatingLevelsFixture.RatedLevelId(3))
            .Build());

        var userService = FakeCurrentUserService.AsController();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 3 };
        var handler = new ApproveEvaluationCommandHandler(
            repository,
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<ApproveEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new ApproveEvaluationCommand(1, 1, "Odobreno"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Store[1].ExcludedFromCompensation.Should().BeFalse();
    }
}
