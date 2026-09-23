using FluentAssertions;
using NSubstitute;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Auth;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Commands.SelectRole;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class SelectRoleCommandHandlerTests
{
    private static readonly Guid CurrentSessionId = Guid.NewGuid();

    private readonly FakeUserRepository userRepository = new();
    private readonly IJwtTokenService jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly FakeCurrentUserService currentUserService = new()
    {
        UserId = 1,
        Roles = [RoleCodes.Evaluator],
        SessionId = CurrentSessionId,
    };

    private readonly SelectRoleCommandHandler handler;

    public SelectRoleCommandHandlerTests()
    {
        this.userRepository.Users[1] = AuthTestUsers.WithRoles(
            1,
            "user@local.dev",
            RoleCodes.Evaluator,
            RoleCodes.Controller);
        this.jwtTokenService.GenerateAccessToken(Arg.Any<User>(), Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>())
            .Returns("access");
        this.jwtTokenService.GenerateRefreshToken().Returns("refresh");

        this.handler = new SelectRoleCommandHandler(
            this.currentUserService,
            this.userRepository,
            new AuthSessionIssuer(this.userRepository, new FakeEmployeeRepository(), this.jwtTokenService));
    }

    [Fact]
    public async Task Handle_MovesTheSessionIntoTheChosenRoleAndRemembersIt()
    {
        var result = await this.handler.Handle(new SelectRoleCommand("controller"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.User.ActiveRole.Should().Be(RoleCodes.Controller);
        this.jwtTokenService.Received(1).GenerateAccessToken(
            Arg.Any<User>(),
            Arg.Is<IEnumerable<string>>(roles => roles.SequenceEqual(new[] { RoleCodes.Controller })),
            Arg.Any<Guid>());
        this.userRepository.RefreshTokens.Single().ActiveRoleCode.Should().Be(RoleCodes.Controller);
        this.userRepository.Users[1].LastActiveRoleCode.Should().Be(RoleCodes.Controller);
    }

    [Fact]
    public async Task Handle_RevokesOnlyTheSessionThatAsked()
    {
        var replaced = Session(CurrentSessionId);
        var otherDevice = Session(Guid.NewGuid());
        this.userRepository.RefreshTokens.AddRange([replaced, otherDevice]);

        var result = await this.handler.Handle(new SelectRoleCommand(RoleCodes.Evaluator), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        replaced.RevokedAt.Should().NotBeNull();
        otherDevice.RevokedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(RoleCodes.Admin)]
    [InlineData("")]
    public async Task Handle_RoleNotAssigned_FailsWithoutIssuingTokens(string roleCode)
    {
        var result = await this.handler.Handle(new SelectRoleCommand(roleCode), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.RoleNotAssigned);
        this.userRepository.RefreshTokens.Should().BeEmpty();
        this.userRepository.Users[1].LastActiveRoleCode.Should().BeNull();
        this.jwtTokenService.DidNotReceiveWithAnyArgs().GenerateAccessToken(default!, default!, default);
    }

    private static RefreshToken Session(Guid sessionId) => new()
    {
        UserId = 1,
        SessionId = sessionId,
        TokenHash = LoginCommandHandler.HashToken(sessionId.ToString()),
        ExpiresAt = DateTime.UtcNow.AddDays(1),
    };
}
