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
            new UpdateAdminUserCommand(1, "user@local.dev", true, ["EVALUATOR", "CONTROLLER"], ControllerEmployeeId: null, Version: 0),
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
            new UpdateAdminUserCommand(1, "user@local.dev", staysActive, ["EMPLOYEE"], ControllerEmployeeId: null, Version: 0),
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
            new UpdateAdminUserCommand(1, "user@local.dev", true, [], ControllerEmployeeId: null, Version: 0),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.UserRolesRequired);
    }

    [Theory]
    [InlineData(false, "ADMIN")]
    [InlineData(true, "EMPLOYEE")]
    public async Task Handle_LastActiveAdministrator_CannotLoseTheRole(bool staysActive, string remainingRole)
    {
        var userRepository = new FakeUserRepository();
        var admin = Administrator(1, "admin@local.dev", isActive: true);
        userRepository.Users[1] = admin;

        var result = await CreateHandler(userRepository).Handle(
            new UpdateAdminUserCommand(1, "admin@local.dev", staysActive, [remainingRole], ControllerEmployeeId: null, Version: 0),
            CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.LastActiveAdministrator);
        admin.IsActive.Should().BeTrue();
        admin.UserRoles.Select(ur => ur.Role.Code).Should().Equal("ADMIN");
    }

    [Fact]
    public async Task Handle_InactiveOtherAdministrator_DoesNotCount()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users[1] = Administrator(1, "admin@local.dev", isActive: true);
        userRepository.Users[2] = Administrator(2, "former@local.dev", isActive: false);

        var result = await CreateHandler(userRepository).Handle(
            new UpdateAdminUserCommand(1, "admin@local.dev", false, ["ADMIN"], ControllerEmployeeId: null, Version: 0),
            CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.LastActiveAdministrator);
    }

    [Fact]
    public async Task Handle_AnotherActiveAdministrator_AllowsHandingOver()
    {
        var userRepository = new FakeUserRepository();
        var admin = Administrator(1, "admin@local.dev", isActive: true);
        userRepository.Users[1] = admin;
        userRepository.Users[2] = Administrator(2, "second@local.dev", isActive: true);

        var result = await CreateHandler(userRepository).Handle(
            new UpdateAdminUserCommand(1, "admin@local.dev", true, ["PAYROLL"], ControllerEmployeeId: null, Version: 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        admin.UserRoles.Select(ur => ur.RoleId).Should().Equal(2);
    }

    [Fact]
    public async Task Handle_AddingARoleThatActsAsAnEmployee_ToAnUnlinkedAccount_IsRefused()
    {
        var userRepository = new FakeUserRepository();
        var user = Administrator(1, "admin@local.dev", isActive: true);
        userRepository.Users[1] = user;
        userRepository.Users[2] = Administrator(2, "second@local.dev", isActive: true);

        var result = await CreateHandler(userRepository).Handle(
            new UpdateAdminUserCommand(1, "admin@local.dev", true, ["ADMIN", "CONTROLLER"], ControllerEmployeeId: null, Version: 0),
            CancellationToken.None);

        result.Error.Should().Be(ErrorCodes.EmployeeRequiredForRoles);
        user.UserRoles.Select(ur => ur.Role.Code).Should().Equal("ADMIN");
    }

    [Fact]
    public async Task Handle_UnlinkedAccountThatAlreadyHadTheRole_CanStillBeEdited()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users[1] = new User
        {
            Id = 1,
            Email = "old@local.dev",
            IsActive = true,
            UserRoles = { new UserRole { UserId = 1, RoleId = 5, Role = new Role { Id = 5, Code = "EMPLOYEE", Name = "Zaposleni" } } },
        };

        var result = await CreateHandler(userRepository).Handle(
            new UpdateAdminUserCommand(1, "renamed@local.dev", true, ["EMPLOYEE"], ControllerEmployeeId: null, Version: 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("renamed@local.dev");
    }

    private static UpdateAdminUserCommandHandler CreateHandler(FakeUserRepository userRepository) =>
        new(
            userRepository,
            new FakeEmployeeRepository(),
            new FakeEvaluatorSettingsRepository(),
            new FakeRoleLookup());

    private static User Administrator(long id, string email, bool isActive) => new()
    {
        Id = id,
        Email = email,
        IsActive = isActive,
        UserRoles = { new UserRole { UserId = id, RoleId = 1, Role = new Role { Id = 1, Code = "ADMIN", Name = "Administrator" } } },
    };
}
