using FluentAssertions;
using VariableCompensation.Application.Auth.Commands.UpdateAdminUser;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class UpdateAdminUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesRoles_ReturnsSortedRoleCodes()
    {
        var userRepository = new FakeUserRepository();
        var user = new User
        {
            Id = 1,
            Email = "user@local.dev",
            IsActive = true,
            UserRoles =
            {
                new UserRole
                {
                    RoleId = 3,
                    Role = new Role { Id = 3, Code = "EVALUATOR", Name = "Ocenjivač" },
                },
            },
        };
        userRepository.Users[1] = user;

        var employeeRepository = new FakeEmployeeRepository();
        employeeRepository.EmployeesByUserId[1] = new Employee { Id = 7 };
        var evaluatorSettings = new FakeEvaluatorSettingsRepository();
        evaluatorSettings.Store[7] = new EvaluatorSettings { EmployeeId = 7, ControllerEmployeeId = 9 };

        var handler = new UpdateAdminUserCommandHandler(
            userRepository,
            employeeRepository,
            evaluatorSettings,
            new FakeRoleLookup());

        var result = await handler.Handle(
            new UpdateAdminUserCommand(1, "user@local.dev", true, ["EVALUATOR", "CONTROLLER"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().Equal("CONTROLLER", "EVALUATOR");
        user.UserRoles.Select(ur => ur.Role.Code).Should().BeEquivalentTo(["EVALUATOR", "CONTROLLER"]);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Handle_Deactivation_SignsOutEverySession(bool staysActive, bool expectSignedOut)
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users[1] = new User
        {
            Id = 1,
            Email = "user@local.dev",
            IsActive = true,
            UserRoles = { new UserRole { RoleId = 5, Role = new Role { Id = 5, Code = "EMPLOYEE", Name = "Zaposleni" } } },
        };
        var token = new RefreshToken
        {
            UserId = 1,
            SessionId = Guid.NewGuid(),
            TokenHash = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        userRepository.RefreshTokens.Add(token);

        var handler = new UpdateAdminUserCommandHandler(
            userRepository,
            new FakeEmployeeRepository(),
            new FakeEvaluatorSettingsRepository(),
            new FakeRoleLookup());

        var result = await handler.Handle(
            new UpdateAdminUserCommand(1, "user@local.dev", staysActive, ["EMPLOYEE"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (token.RevokedAt is not null).Should().Be(expectSignedOut);
    }

    [Fact]
    public async Task Handle_EmptyRoles_ReturnsError()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users[1] = new User { Id = 1, Email = "user@local.dev", IsActive = true };

        var handler = new UpdateAdminUserCommandHandler(
            userRepository,
            new FakeEmployeeRepository(),
            new FakeEvaluatorSettingsRepository(),
            new FakeRoleLookup());

        var result = await handler.Handle(
            new UpdateAdminUserCommand(1, "user@local.dev", true, []),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.UserRolesRequired);
    }
}
