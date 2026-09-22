using FluentAssertions;
using NSubstitute;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Auth.Commands.RegisterUser;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class RegisterUserCommandHandlerTests
{
    /// <summary>
    /// Accounts are created by an administrator. Tokens issued here would sign the administrator in as the new
    /// user.
    /// </summary>
    [Fact]
    public async Task Handle_CreatesTheUser_WithoutSigningAnyoneIn()
    {
        var userRepository = new FakeUserRepository();
        var handler = new RegisterUserCommandHandler(userRepository, new FakeRoleLookup(), Substitute.For<IPasswordHasher>());

        var result = await handler.Handle(
            new RegisterUserCommand("new.user@local.dev", "Password123!", ["EMPLOYEE"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("new.user@local.dev");
        result.Value.Roles.Should().Equal("EMPLOYEE");
        userRepository.RefreshTokens.Should().BeEmpty();
    }
}
