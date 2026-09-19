using FluentAssertions;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Evaluation;

[Trait("Category", "Unit")]
public class EvaluationAccessServiceTests
{
    private readonly FakeCurrentUserService userService = new();
    private readonly FakeCurrentEmployeeContext employeeContext = new();
    private readonly EvaluationAccessService service;

    public EvaluationAccessServiceTests()
    {
        this.service = new EvaluationAccessService(this.userService, this.employeeContext);
    }

    [Fact]
    public async Task EnsureCanViewAsync_Admin_AlwaysSucceeds()
    {
        this.userService.Roles = [RoleCodes.Admin];
        var evaluation = new EvaluationBuilder().Build();

        var result = await this.service.EnsureCanViewAsync(evaluation, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureCanViewAsync_EmployeeOwnEvaluation_Succeeds()
    {
        this.userService.Roles = [RoleCodes.Employee];
        this.employeeContext.EmployeeId = 10;
        var evaluation = new EvaluationBuilder().WithEmployee(10).Build();

        var result = await this.service.EnsureCanViewAsync(evaluation, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureCanViewAsync_EmployeeOtherEvaluation_Fails()
    {
        this.userService.Roles = [RoleCodes.Employee];
        this.employeeContext.EmployeeId = 10;
        var evaluation = new EvaluationBuilder().WithEmployee(99).Build();

        var result = await this.service.EnsureCanViewAsync(evaluation, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.EvaluationAccessDenied);
    }

    [Fact]
    public async Task EnsureCanEditDraftAsync_EvaluatorNotAssigned_Fails()
    {
        this.userService.Roles = [RoleCodes.Evaluator];
        this.employeeContext.EmployeeId = 2;
        var evaluation = new EvaluationBuilder().WithEvaluator(99).Build();

        var result = await this.service.EnsureCanEditDraftAsync(evaluation, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.EvaluatorOnlyOwnAssigned);
    }

    [Fact]
    public async Task EnsureCanReviewAsync_ControllerNotAssigned_Fails()
    {
        this.userService.Roles = [RoleCodes.Controller];
        this.employeeContext.EmployeeId = 3;
        var evaluation = new EvaluationBuilder().WithController(99).Build();

        var result = await this.service.EnsureCanReviewAsync(evaluation, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.ControllerOnlyOwnAssigned);
    }

    [Fact]
    public async Task EnsureCanViewAsync_UserWithoutEmployee_Fails()
    {
        this.userService.Roles = [RoleCodes.Employee];
        this.employeeContext.EmployeeId = null;
        var evaluation = new EvaluationBuilder().Build();

        var result = await this.service.EnsureCanViewAsync(evaluation, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.UserNotLinkedToEmployee);
    }

    [Fact]
    public async Task ResolveListFiltersAsync_Evaluator_ScopesToSelf()
    {
        this.userService.Roles = [RoleCodes.Evaluator];
        this.employeeContext.EmployeeId = 2;

        var (employeeId, evaluatorEmployeeId, controllerEmployeeId) =
            await this.service.ResolveListFiltersAsync(10, null, null, CancellationToken.None);

        employeeId.Should().Be(10);
        evaluatorEmployeeId.Should().Be(2);
        controllerEmployeeId.Should().BeNull();
    }

    [Fact]
    public async Task ResolveListFiltersAsync_Controller_ScopesToSelf()
    {
        this.userService.Roles = [RoleCodes.Controller];
        this.employeeContext.EmployeeId = 3;

        var (_, evaluatorEmployeeId, controllerEmployeeId) =
            await this.service.ResolveListFiltersAsync(null, 2, null, CancellationToken.None);

        evaluatorEmployeeId.Should().Be(2);
        controllerEmployeeId.Should().Be(3);
    }

    [Fact]
    public async Task ResolveListFiltersAsync_PayrollOnly_SeesNoEvaluations()
    {
        this.userService.Roles = [RoleCodes.Payroll];
        this.employeeContext.EmployeeId = 5;

        var (employeeId, _, _) =
            await this.service.ResolveListFiltersAsync(null, null, null, CancellationToken.None);

        employeeId.Should().Be(-1);
    }
}
