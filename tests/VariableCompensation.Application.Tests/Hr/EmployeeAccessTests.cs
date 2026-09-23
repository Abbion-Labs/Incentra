using FluentAssertions;
using NSubstitute;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Hr.Employees.Queries;
using VariableCompensation.Application.Hr.Services;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Hr;

[Trait("Category", "Unit")]
public class EmployeeAccessTests
{
    private const long CurrentEmployeeId = 10;
    private const long OtherEmployeeId = 20;

    private readonly FakeCurrentUserService userService = new();
    private readonly FakeCurrentEmployeeContext employeeContext = new() { EmployeeId = CurrentEmployeeId };
    private readonly IEmployeeRepository employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IEvaluatorSettingsRepository evaluatorSettingsRepository = Substitute.For<IEvaluatorSettingsRepository>();
    private readonly EmployeeAccessService accessService;

    public EmployeeAccessTests()
    {
        this.accessService = new EmployeeAccessService(
            this.userService,
            this.employeeContext,
            new ControllerSupervisionService(this.evaluatorSettingsRepository, this.employeeRepository));
    }

    [Fact]
    public async Task EnsureCanView_Admin_CanViewAnyone()
    {
        this.userService.Roles = [RoleCodes.Admin];

        var result = await this.accessService.EnsureCanViewAsync(Employee(OtherEmployeeId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(RoleCodes.Employee)]
    [InlineData(RoleCodes.Payroll)]
    [InlineData(RoleCodes.Evaluator)]
    [InlineData(RoleCodes.Controller)]
    public async Task EnsureCanView_OwnRecord_IsAllowedForEveryRole(string role)
    {
        this.userService.Roles = [role];

        var result = await this.accessService.EnsureCanViewAsync(Employee(CurrentEmployeeId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(RoleCodes.Employee)]
    [InlineData(RoleCodes.Payroll)]
    public async Task EnsureCanView_OtherEmployee_IsDeniedForPlainRoles(string role)
    {
        this.userService.Roles = [role];

        var result = await this.accessService.EnsureCanViewAsync(Employee(OtherEmployeeId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.EmployeeAccessDenied);
    }

    [Fact]
    public async Task EnsureCanView_Evaluator_CanViewOwnSubordinate()
    {
        this.userService.Roles = [RoleCodes.Evaluator];
        var subordinate = new EmployeeBuilder().WithId(OtherEmployeeId).WithEvaluator(CurrentEmployeeId).Build();

        var result = await this.accessService.EnsureCanViewAsync(subordinate, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureCanView_Evaluator_CannotViewSomeoneElsesSubordinate()
    {
        this.userService.Roles = [RoleCodes.Evaluator];
        var other = new EmployeeBuilder().WithId(OtherEmployeeId).WithEvaluator(99).Build();

        var result = await this.accessService.EnsureCanViewAsync(other, CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.EmployeeAccessDenied);
    }

    [Fact]
    public async Task EnsureCanView_Controller_CanViewEmployeeOfSupervisedEvaluator()
    {
        this.userService.Roles = [RoleCodes.Controller];
        const long evaluatorId = 30;
        var employee = new EmployeeBuilder().WithId(OtherEmployeeId).WithEvaluator(evaluatorId).Build();
        this.employeeRepository.FindByIdWithEvaluatorAsync(OtherEmployeeId, Arg.Any<CancellationToken>()).Returns(employee);
        this.evaluatorSettingsRepository.FindByEmployeeIdAsync(evaluatorId, Arg.Any<CancellationToken>())
            .Returns(new EvaluatorSettings { EmployeeId = evaluatorId, ControllerEmployeeId = CurrentEmployeeId });

        var result = await this.accessService.EnsureCanViewAsync(employee, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureCanView_Controller_CannotViewEmployeeOfUnsupervisedEvaluator()
    {
        this.userService.Roles = [RoleCodes.Controller];
        const long evaluatorId = 30;
        var employee = new EmployeeBuilder().WithId(OtherEmployeeId).WithEvaluator(evaluatorId).Build();
        this.employeeRepository.FindByIdWithEvaluatorAsync(OtherEmployeeId, Arg.Any<CancellationToken>()).Returns(employee);
        this.evaluatorSettingsRepository.FindByEmployeeIdAsync(evaluatorId, Arg.Any<CancellationToken>())
            .Returns(new EvaluatorSettings { EmployeeId = evaluatorId, ControllerEmployeeId = 99 });

        var result = await this.accessService.EnsureCanViewAsync(employee, CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.EmployeeAccessDenied);
    }

    [Fact]
    public async Task EnsureCanView_UserWithoutLinkedEmployee_IsDenied()
    {
        this.userService.Roles = [RoleCodes.Employee];
        this.employeeContext.EmployeeId = null;

        var result = await this.accessService.EnsureCanViewAsync(Employee(OtherEmployeeId), CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.UserNotLinkedToEmployee);
    }

    [Fact]
    public async Task GetEmployeeById_OtherEmployee_ReturnsAccessDenied()
    {
        this.userService.Roles = [RoleCodes.Employee];
        this.employeeRepository.FindByIdAsync(OtherEmployeeId, Arg.Any<CancellationToken>()).Returns(Employee(OtherEmployeeId));
        var handler = new GetEmployeeByIdQueryHandler(this.employeeRepository, this.accessService);

        var result = await handler.Handle(new GetEmployeeByIdQuery(OtherEmployeeId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.EmployeeAccessDenied);
    }

    [Fact]
    public async Task GetEmployeeById_OwnRecord_ReturnsIt()
    {
        this.userService.Roles = [RoleCodes.Employee];
        this.employeeRepository.FindByIdAsync(CurrentEmployeeId, Arg.Any<CancellationToken>()).Returns(Employee(CurrentEmployeeId));
        var handler = new GetEmployeeByIdQueryHandler(this.employeeRepository, this.accessService);

        var result = await handler.Handle(new GetEmployeeByIdQuery(CurrentEmployeeId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(CurrentEmployeeId);
    }

    [Fact]
    public async Task GetEmployeeById_UnknownId_ReturnsNotFound()
    {
        this.userService.Roles = [RoleCodes.Admin];
        var handler = new GetEmployeeByIdQueryHandler(this.employeeRepository, this.accessService);

        var result = await handler.Handle(new GetEmployeeByIdQuery(999), CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.EmployeeNotFound);
    }

    [Theory]
    [InlineData(RoleCodes.Employee)]
    [InlineData(RoleCodes.Payroll)]
    public async Task GetEmployees_PlainRoles_SeeOnlyTheirOwnRecord(string role)
    {
        this.userService.Roles = [role];
        this.employeeRepository.FindByIdAsync(CurrentEmployeeId, Arg.Any<CancellationToken>()).Returns(Employee(CurrentEmployeeId));
        var handler = this.CreateListHandler();

        var result = await handler.Handle(ListQuery(), CancellationToken.None);

        result.Items.Select(e => e.Id).Should().Equal(CurrentEmployeeId);
        result.TotalCount.Should().Be(1);
        await this.employeeRepository.DidNotReceiveWithAnyArgs().GetPagedAsync(
            default, default, default, default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task GetEmployees_PlainRoleWithoutLinkedEmployee_SeesNothing()
    {
        this.userService.Roles = [RoleCodes.Payroll];
        this.employeeContext.EmployeeId = null;
        var handler = this.CreateListHandler();

        var result = await handler.Handle(ListQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetEmployees_Admin_QueriesTheRepository()
    {
        this.userService.Roles = [RoleCodes.Admin];
        this.employeeRepository.GetPagedAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<long?>(), Arg.Any<string?>(), Arg.Any<bool?>(),
                Arg.Any<long?>(), Arg.Any<short?>(), Arg.Any<byte?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<Employee> { Employee(1), Employee(2) }, 2));
        var handler = this.CreateListHandler();

        var result = await handler.Handle(ListQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(RoleCodes.Evaluator)]
    [InlineData(RoleCodes.Controller)]
    public async Task GetEmployees_ScopesToTheRoleOfTheSession(string sessionRole)
    {
        this.userService.Roles = [sessionRole];
        this.ReturnNoEmployeesFromTheRepository();
        var handler = this.CreateListHandler();

        await handler.Handle(ListQuery(), CancellationToken.None);

        await this.employeeRepository.Received(1).GetPagedAsync(
            1,
            20,
            null,
            sessionRole == RoleCodes.Evaluator ? CurrentEmployeeId : null,
            null,
            null,
            sessionRole == RoleCodes.Controller ? CurrentEmployeeId : null,
            null,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEmployees_AdminSession_QueriesWithoutAScope()
    {
        this.userService.Roles = [RoleCodes.Admin];
        this.ReturnNoEmployeesFromTheRepository();
        var handler = this.CreateListHandler();

        await handler.Handle(ListQuery(), CancellationToken.None);

        await this.employeeRepository.Received(1).GetPagedAsync(
            1, 20, null, null, null, null, null, null, null, null, Arg.Any<CancellationToken>());
    }

    private static Employee Employee(long id) => new EmployeeBuilder().WithId(id).Build();

    private static GetEmployeesQuery ListQuery() => new(1, 20, null, null, null, null, null, null, null);

    private GetEmployeesQueryHandler CreateListHandler() =>
        new(this.employeeRepository, this.userService, this.employeeContext);

    private void ReturnNoEmployeesFromTheRepository() =>
        this.employeeRepository.GetPagedAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<long?>(), Arg.Any<string?>(), Arg.Any<bool?>(),
                Arg.Any<long?>(), Arg.Any<short?>(), Arg.Any<byte?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<Employee>(), 0));
}
