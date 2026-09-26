using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VariableCompensation.Application.Evaluation.Commands;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fakes;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Application.Tests.Evaluation.Handlers;

[Trait("Category", "Unit")]
public class SubmitEvaluationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidDraft_SubmitsEvaluation()
    {
        var repository = new FakeEvaluationRepository();
        var evaluation = new EvaluationBuilder().WithAgreedPlan()
            .WithId(1)
            .WithEvaluator(2)
            .WithController(3)
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .AddMeasure(RatingLevelsFixture.RatedLevelId(4))
            .Build();
        repository.Seed(evaluation);

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var handler = new SubmitEvaluationCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<SubmitEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new SubmitEvaluationCommand(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(EvaluationStatus.Submitted));
        repository.Store[1].Status.Should().Be(EvaluationStatus.Submitted);
    }

    [Fact]
    public async Task Handle_EvaluatorWithoutController_ApprovesOnSubmission()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder().WithAgreedPlan()
            .WithId(1)
            .WithEvaluator(2)
            .WithController(null)
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .AddMeasure(RatingLevelsFixture.RatedLevelId(4))
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var notifications = new FakeEvaluationNotificationService();
        var handler = new SubmitEvaluationCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext),
            notifications,
            NullLogger<SubmitEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new SubmitEvaluationCommand(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = repository.Store[1];
        stored.Status.Should().Be(EvaluationStatus.Approved);
        stored.ApprovedAt.Should().NotBeNull();
        stored.ExcludedFromCompensation.Should().BeFalse();
        stored.StatusHistory.Select(h => h.ToStatus)
            .Should().Equal(nameof(EvaluationStatus.Submitted), nameof(EvaluationStatus.Approved));
    }

    [Fact]
    public async Task Handle_WithoutMeasures_ReturnsError()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder().WithAgreedPlan()
            .WithId(1)
            .WithEvaluator(2)
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var handler = new SubmitEvaluationCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<SubmitEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new SubmitEvaluationCommand(1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.SubmitRequiresMeasure);
    }

    [Fact]
    public async Task Handle_ConditionsNotMetWithoutComment_ReturnsError()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder().WithAgreedPlan()
            .WithId(1)
            .WithEvaluator(2)
            .WithConditionsFulfilled(false)
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var handler = new SubmitEvaluationCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<SubmitEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new SubmitEvaluationCommand(1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.SubmitRequiresEvaluatorComment);
    }

    [Fact]
    public async Task Handle_ConditionsNotMetWithComment_SubmitsWithoutMeasures()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder().WithAgreedPlan()
            .WithId(1)
            .WithEvaluator(2)
            .WithController(3)
            .WithConditionsFulfilled(false)
            .WithConditionsNotMetComment("Uslovi nisu ispunjeni.")
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var handler = new SubmitEvaluationCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<SubmitEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new SubmitEvaluationCommand(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Store[1].OverallAverage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_VersionConflict_ReturnsError()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder().WithAgreedPlan()
            .WithId(1)
            .WithEvaluator(2)
            .WithVersion(2)
            .AddGoal(RatingLevelsFixture.RatedLevelId(3))
            .AddMeasure(RatingLevelsFixture.RatedLevelId(3))
            .Build());

        var userService = FakeCurrentUserService.AsEvaluator();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 2 };
        var handler = new SubmitEvaluationCommandHandler(
            repository,
            new FakeEvaluationLookupRepository(),
            new EvaluationScoringService(),
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<SubmitEvaluationCommandHandler>.Instance);

        var result = await handler.Handle(new SubmitEvaluationCommand(1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.EvaluationVersionConflict);
    }
}
