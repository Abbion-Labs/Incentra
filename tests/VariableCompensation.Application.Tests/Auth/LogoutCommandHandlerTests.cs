using FluentAssertions;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Commands.Logout;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class LogoutCommandHandlerTests
{
    private readonly FakeUserRepository userRepository = new();
    private readonly LogoutCommandHandler handler;

    public LogoutCommandHandlerTests()
    {
        this.handler = new LogoutCommandHandler(this.userRepository);
    }

    [Fact]
    public async Task Handle_ActiveToken_RevokesIt()
    {
        var token = new RefreshToken
        {
            UserId = 1,
            TokenHash = LoginCommandHandler.HashToken("plain-token"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        this.userRepository.RefreshTokens.Add(token);

        var result = await this.handler.Handle(new LogoutCommand("plain-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_SignsOutTheWholeSession_ButNotTheUsersOtherSessions()
    {
        var sessionId = Guid.NewGuid();
        var presented = new RefreshToken
        {
            UserId = 1,
            SessionId = sessionId,
            TokenHash = LoginCommandHandler.HashToken("plain-token"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        var sameSession = new RefreshToken
        {
            UserId = 1,
            SessionId = sessionId,
            TokenHash = "issued-to-another-tab",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        var otherDevice = new RefreshToken
        {
            UserId = 1,
            SessionId = Guid.NewGuid(),
            TokenHash = "other-device",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        this.userRepository.RefreshTokens.AddRange([presented, sameSession, otherDevice]);

        await this.handler.Handle(new LogoutCommand("plain-token"), CancellationToken.None);

        presented.RevokedAt.Should().NotBeNull();
        sameSession.RevokedAt.Should().NotBeNull();
        otherDevice.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_KeepsOriginalRevocationTime()
    {
        var revokedAt = DateTime.UtcNow.AddHours(-2);
        var token = new RefreshToken
        {
            UserId = 1,
            TokenHash = LoginCommandHandler.HashToken("plain-token"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = revokedAt,
        };
        this.userRepository.RefreshTokens.Add(token);

        var result = await this.handler.Handle(new LogoutCommand("plain-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAt.Should().Be(revokedAt);
    }

    [Theory]
    [InlineData("unknown-token")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_UnknownOrBlankToken_StillSucceeds(string refreshToken)
    {
        var result = await this.handler.Handle(new LogoutCommand(refreshToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
