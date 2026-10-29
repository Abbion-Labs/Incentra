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
            new FakeEmployeeRepository(),
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
            new FakeEmployeeRepository(),
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

    [Fact]
    public async Task Handle_EmployeeHandedToAnotherEvaluator_MovesTheRevisionToThem()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEmployee(10)
            .WithEvaluator(2)
            .WithController(3)
            .WithStatus(EvaluationStatus.Submitted)
            .Build());
        var employees = new FakeEmployeeRepository();
        employees.EmployeesById[10] = new EmployeeBuilder().WithId(10).WithEvaluator(7).Build();

        var result = await CreateHandler(repository, employees).Handle(
            new ReturnEvaluationForRevisionCommand(1, 1, "Dopuniti merila"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = repository.Store[1];
        stored.Status.Should().Be(EvaluationStatus.Draft);
        stored.EvaluatorEmployeeId.Should().Be(7);
        stored.ControllerEmployeeId.Should().Be(3);
    }

    [Fact]
    public async Task Handle_SameEvaluator_KeepsTheAssignment()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEmployee(10)
            .WithEvaluator(2)
            .WithController(3)
            .WithStatus(EvaluationStatus.Submitted)
            .Build());
        var employees = new FakeEmployeeRepository();
        employees.EmployeesById[10] = new EmployeeBuilder().WithId(10).WithEvaluator(2).Build();

        var result = await CreateHandler(repository, employees).Handle(
            new ReturnEvaluationForRevisionCommand(1, 1, "Dopuniti merila"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Store[1].EvaluatorEmployeeId.Should().Be(2);
        repository.Store[1].Version.Should().Be(2);
    }

    private static ReturnEvaluationForRevisionCommandHandler CreateHandler(
        FakeEvaluationRepository repository,
        FakeEmployeeRepository employees)
    {
        var userService = FakeCurrentUserService.AsController();
        var employeeContext = new FakeCurrentEmployeeContext { EmployeeId = 3 };
        return new ReturnEvaluationForRevisionCommandHandler(
            repository,
            employees,
            userService,
            new EvaluationAccessService(userService, employeeContext),
            new FakeEvaluationNotificationService(),
            NullLogger<ReturnEvaluationForRevisionCommandHandler>.Instance);
    }
}
