using FluentAssertions;
using NSubstitute;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Auth.Commands.RegisterUser;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class RegisterUserCommandHandlerTests
{
    private readonly FakeUserRepository userRepository = new();
    private readonly FakeEmployeeRepository employeeRepository = new();
    private readonly FakeEvaluatorSettingsRepository evaluatorSettings = new();

    /// <summary>
    /// Accounts are created by an administrator. Tokens issued here would sign the administrator in as the new
    /// user.
    /// </summary>
    [Fact]
    public async Task Handle_CreatesTheUser_WithoutSigningAnyoneIn()
    {
        var result = await this.Register(["PAYROLL"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("new.user@local.dev");
        result.Value.Roles.Should().Equal("PAYROLL");
        this.userRepository.RefreshTokens.Should().BeEmpty();
    }

    [Theory]
    [InlineData("EMPLOYEE")]
    [InlineData("EVALUATOR")]
    [InlineData("CONTROLLER")]
    public async Task Handle_RoleThatActsAsAnEmployee_WithoutAnEmployee_IsRefused(string roleCode)
    {
        var result = await this.Register([roleCode]);

        result.Error.Should().Be(ErrorCodes.EmployeeRequiredForRoles);
        this.userRepository.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithAnEmployee_LinksTheAccountInTheSameStep()
    {
        var employee = this.Employee(7);

        var result = await this.Register(["EMPLOYEE"], employeeId: 7);

        result.IsSuccess.Should().BeTrue();
        employee.User.Should().NotBeNull();
        employee.User!.Email.Should().Be("new.user@local.dev");
    }

    [Fact]
    public async Task Handle_Evaluator_GetsEvaluatorSettingsWithTheChosenController()
    {
        this.Employee(7);

        var result = await this.Register(["EMPLOYEE", "EVALUATOR"], employeeId: 7, controllerEmployeeId: null);

        result.IsSuccess.Should().BeTrue();
        this.evaluatorSettings.Store.Should().ContainKey(7);
        this.evaluatorSettings.Store[7].ControllerEmployeeId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EmployeeWhoAlreadyHasAnAccount_IsRefused()
    {
        this.Employee(7).UserId = 99;

        var result = await this.Register(["EMPLOYEE"], employeeId: 7);

        result.Error.Should().Be(ErrorCodes.EmployeeAlreadyHasAccount);
        this.userRepository.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InactiveEmployee_IsRefused()
    {
        this.Employee(7).IsActive = false;

        var result = await this.Register(["EMPLOYEE"], employeeId: 7);

        result.Error.Should().Be(ErrorCodes.EmployeeInactive);
    }

    private Employee Employee(long id)
    {
        var employee = new Employee { Id = id, FirstName = "Ana", LastName = "Anić", IsActive = true };
        this.employeeRepository.EmployeesById[id] = employee;
        return employee;
    }

    private Task<CSharpFunctionalExtensions.Result<VariableCompensation.Application.Auth.Models.UserProfileResponse>> Register(
        IReadOnlyList<string> roles,
        long? employeeId = null,
        long? controllerEmployeeId = null) =>
        new RegisterUserCommandHandler(
                this.userRepository,
                new FakeRoleLookup(),
                Substitute.For<IPasswordHasher>(),
                this.employeeRepository,
                this.evaluatorSettings)
            .Handle(
                new RegisterUserCommand("new.user@local.dev", "Password123!", roles, employeeId, controllerEmployeeId),
                CancellationToken.None);
}
