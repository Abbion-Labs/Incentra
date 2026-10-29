using FluentAssertions;
using NSubstitute;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Auth.Commands.ChangePassword;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class ChangePasswordCommandHandlerTests
{
    private readonly FakeUserRepository userRepository = new();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly Guid currentSession = Guid.NewGuid();

    public ChangePasswordCommandHandlerTests()
    {
        this.userRepository.Users[1] = new User { Id = 1, Email = "user@local.dev", PasswordHash = "hash", IsActive = true };
        this.passwordHasher.Verify("current-password", "hash").Returns(true);
        this.passwordHasher.Hash("new-password").Returns("new-hash");
    }

    [Fact]
    public async Task Handle_SignsOutOtherDevices_AndKeepsTheCurrentSession()
    {
        var current = this.AddToken(this.currentSession);
        var otherDevice = this.AddToken(Guid.NewGuid());

        var result = await this.CreateHandler(this.currentSession)
            .Handle(new ChangePasswordCommand("current-password", "new-password"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        this.userRepository.Users[1].PasswordHash.Should().Be("new-hash");
        current.RevokedAt.Should().BeNull();
        otherDevice.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithoutAKnownSession_SignsOutEverySession()
    {
        var token = this.AddToken(this.currentSession);

        var result = await this.CreateHandler(sessionId: null)
            .Handle(new ChangePasswordCommand("current-password", "new-password"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_SignsNobodyOut()
    {
        var otherDevice = this.AddToken(Guid.NewGuid());

        var result = await this.CreateHandler(this.currentSession)
            .Handle(new ChangePasswordCommand("wrong-password", "new-password"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        otherDevice.RevokedAt.Should().BeNull();
    }

    private ChangePasswordCommandHandler CreateHandler(Guid? sessionId) =>
        new(new FakeCurrentUserService { UserId = 1, SessionId = sessionId }, this.userRepository, this.passwordHasher);

    private RefreshToken AddToken(Guid sessionId)
    {
        var token = new RefreshToken
        {
            UserId = 1,
            SessionId = sessionId,
            TokenHash = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        this.userRepository.RefreshTokens.Add(token);
        return token;
    }
}
