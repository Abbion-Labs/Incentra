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
public class ReturnEvaluationForRevisionCommandHandlerTests
{
    [Fact]
    public async Task Handle_Submitted_ReturnsToDraft()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithController(3)
            .WithStatus(EvaluationStatus.Submitted)
            .Build());

        var userService = FakeCurrentUserService.AsController();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 3 };
        var handler = new ReturnEvaluationForRevisionCommandHandler(
            repository,
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<ReturnEvaluationForRevisionCommandHandler>.Instance);

        var result = await handler.Handle(
            new ReturnEvaluationForRevisionCommand(1, 1, "Dopuniti merila"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(EvaluationStatus.Draft));
        repository.Store[1].ControllerComment.Should().Be("Dopuniti merila");
    }

    [Fact]
    public async Task Handle_EmptyComment_ReturnsError()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithController(3)
            .WithStatus(EvaluationStatus.Submitted)
            .Build());

        var userService = FakeCurrentUserService.AsController();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 3 };
        var handler = new ReturnEvaluationForRevisionCommandHandler(
            repository,
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<ReturnEvaluationForRevisionCommandHandler>.Instance);

        var result = await handler.Handle(
            new ReturnEvaluationForRevisionCommand(1, 1, "  "),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.RevisionCommentRequired);
    }
}
