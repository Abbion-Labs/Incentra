using FluentAssertions;
using VariableCompensation.Application.Auth.Commands.UpdateAdminUser;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Entities.Lookup;
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

        var handler = new UpdateAdminUserCommandHandler(
            userRepository,
            new FakeEmployeeRepository(),
            new FakeRoleLookup());

        var result = await handler.Handle(
            new UpdateAdminUserCommand(1, "user@local.dev", true, ["EVALUATOR", "CONTROLLER"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().Equal("CONTROLLER", "EVALUATOR");
        user.UserRoles.Select(ur => ur.Role.Code).Should().BeEquivalentTo(["EVALUATOR", "CONTROLLER"]);
    }

    [Fact]
    public async Task Handle_EmptyRoles_ReturnsError()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users[1] = new User { Id = 1, Email = "user@local.dev", IsActive = true };

        var handler = new UpdateAdminUserCommandHandler(
            userRepository,
            new FakeEmployeeRepository(),
            new FakeRoleLookup());

        var result = await handler.Handle(
            new UpdateAdminUserCommand(1, "user@local.dev", true, []),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.UserRolesRequired);
    }
}
