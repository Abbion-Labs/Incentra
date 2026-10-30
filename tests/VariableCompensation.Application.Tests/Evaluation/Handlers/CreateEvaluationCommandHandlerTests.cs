using FluentAssertions;
using VariableCompensation.Application.Evaluation.Commands;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Evaluation.Handlers;

[Trait("Category", "Unit")]
public class CreateEvaluationCommandHandlerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1999)]
    [InlineData(32000)]
    public async Task AYearOutsideTheUsableRange_IsRefused(short year)
    {
        var repository = new FakeEvaluationRepository();
        var result = await CreateHandler(repository).Handle(
            new CreateEvaluationCommand(EmployeeId: 1, Year: year, Quarter: 1),
            CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.YearInvalid);
        repository.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task NextYear_IsAllowed_SoGoalsCanBeSetAhead()
    {
        var nextYear = (short)(DateTime.UtcNow.Year + 1);

        var result = await CreateHandler(new FakeEvaluationRepository()).Handle(
            new CreateEvaluationCommand(EmployeeId: 1, Year: nextYear, Quarter: 1),
            CancellationToken.None);

        // The year passes; the unknown employee is what stops this one.
        result.Error.Should().Be(ErrorCodes.EmployeeNotFound);
    }

    private static CreateEvaluationCommandHandler CreateHandler(FakeEvaluationRepository repository)
    {
        var userService = FakeCurrentUserService.AsEvaluator();
        return new CreateEvaluationCommandHandler(
            repository,
            new FakeEmployeeRepository(),
            userService,
            new EvaluationAccessService(userService, new FakeCurrentEmployeeContext { EmployeeId = 2 }));
    }
}
