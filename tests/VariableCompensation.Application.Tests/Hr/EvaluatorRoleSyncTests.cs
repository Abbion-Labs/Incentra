using FluentAssertions;
using VariableCompensation.Application.Hr.EvaluatorSettings.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Hr;

[Trait("Category", "Unit")]
public class EvaluatorRoleSyncTests
{
    private const long UserId = 1;
    private const long EmployeeId = 7;
    private const long ControllerId = 9;

    [Fact]
    public async Task GrantingTheRole_CreatesSettingsWithTheDefaultThresholds()
    {
        var (employees, settings) = LinkedEmployee();
        employees.ExistingEmployeeIds.Add(ControllerId);

        var result = await Apply(employees, settings, hasRole: true, controllerEmployeeId: ControllerId);

        result.IsSuccess.Should().BeTrue();
        settings.Store.Should().ContainKey(EmployeeId);
        var created = settings.Store[EmployeeId];
        created.ControllerEmployeeId.Should().Be(ControllerId);
        created.ThresholdDoesNotMeet.Should().Be(EvaluatorRoleSync.DefaultThresholdDoesNotMeet);
        created.ThresholdExceeds.Should().Be(EvaluatorRoleSync.DefaultThresholdExceeds);
        created.PercentExceeds.Should().Be(EvaluatorRoleSync.DefaultPercentExceeds);
    }

    [Fact]
    public async Task GrantingTheRole_ToAnAccountWithoutAnEmployee_IsRejected()
    {
        var result = await Apply(
            new FakeEmployeeRepository(),
            new FakeEvaluatorSettingsRepository(),
            hasRole: true,
            controllerEmployeeId: ControllerId);

        result.Error.Should().Be(ErrorCodes.EvaluatorUserNotLinkedToEmployee);
    }

    [Fact]
    public async Task GrantingTheRole_WithoutAController_IsRejected()
    {
        var (employees, settings) = LinkedEmployee();

        var result = await Apply(employees, settings, hasRole: true, controllerEmployeeId: null);

        result.Error.Should().Be(ErrorCodes.EvaluatorControllerRequired);
    }

    [Fact]
    public async Task GrantingTheRole_WithAControllerThatDoesNotExist_IsRejected()
    {
        var (employees, settings) = LinkedEmployee();

        var result = await Apply(employees, settings, hasRole: true, controllerEmployeeId: 404);

        result.Error.Should().Be(ErrorCodes.ControllerNotFound);
    }

    [Fact]
    public async Task GrantingTheRole_ToSomeoneAlreadyConfigured_LeavesTunedThresholdsAlone()
    {
        var (employees, settings) = LinkedEmployee();
        settings.Store[EmployeeId] = new EvaluatorSettings
        {
            EmployeeId = EmployeeId,
            ControllerEmployeeId = ControllerId,
            ThresholdExceeds = 4.9m,
        };

        var result = await Apply(employees, settings, hasRole: true, controllerEmployeeId: ControllerId);

        result.IsSuccess.Should().BeTrue();
        settings.Store[EmployeeId].ThresholdExceeds.Should().Be(4.9m);
    }

    [Fact]
    public async Task RemovingTheRole_DropsTheSettings()
    {
        var (employees, settings) = LinkedEmployee();
        settings.Store[EmployeeId] = new EvaluatorSettings { EmployeeId = EmployeeId, ControllerEmployeeId = ControllerId };

        var result = await Apply(employees, settings, hasRole: false, controllerEmployeeId: null);

        result.IsSuccess.Should().BeTrue();
        settings.Store.Should().NotContainKey(EmployeeId);
    }

    [Fact]
    public async Task RemovingTheRole_WhileSomeoneStillReportsToThem_IsRejected()
    {
        var (employees, settings) = LinkedEmployee();
        settings.Store[EmployeeId] = new EvaluatorSettings { EmployeeId = EmployeeId, ControllerEmployeeId = ControllerId };
        employees.EvaluatorsWithSubordinates.Add(EmployeeId);

        var result = await Apply(employees, settings, hasRole: false, controllerEmployeeId: null);

        result.Error.Should().Be(ErrorCodes.EvaluatorHasSubordinates);
        settings.Store.Should().ContainKey(EmployeeId);
    }

    [Fact]
    public async Task NotAnEvaluatorAndNeverWas_IsANoOp()
    {
        var (employees, settings) = LinkedEmployee();

        var result = await Apply(employees, settings, hasRole: false, controllerEmployeeId: null);

        result.IsSuccess.Should().BeTrue();
        settings.Store.Should().BeEmpty();
    }

    private static (FakeEmployeeRepository Employees, FakeEvaluatorSettingsRepository Settings) LinkedEmployee()
    {
        var employees = new FakeEmployeeRepository();
        employees.EmployeesByUserId[UserId] = new Employee { Id = EmployeeId };
        return (employees, new FakeEvaluatorSettingsRepository());
    }

    private static Task<CSharpFunctionalExtensions.Result> Apply(
        FakeEmployeeRepository employees,
        FakeEvaluatorSettingsRepository settings,
        bool hasRole,
        long? controllerEmployeeId) =>
        EvaluatorRoleSync.ApplyAsync(UserId, hasRole, controllerEmployeeId, employees, settings, CancellationToken.None);
}
