using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Auth;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Application.Auth.Commands.RefreshToken;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class RefreshTokenCommandHandlerTests
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(30);

    private readonly FakeUserRepository userRepository = new();
    private readonly IJwtTokenService jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly ManualTimeProvider time = new();
    private readonly Guid sessionId = Guid.NewGuid();
    private readonly RefreshTokenCommandHandler handler;
    private int issuedTokens;

    public RefreshTokenCommandHandlerTests()
    {
        this.userRepository.Users[1] = new User { Id = 1, Email = "user@local.dev", IsActive = true };

        this.jwtTokenService.GenerateRefreshToken().Returns(_ => $"issued-{++this.issuedTokens}");
        this.jwtTokenService
            .GenerateAccessToken(Arg.Any<User>(), Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>())
            .Returns(call => $"access-for-{call.ArgAt<Guid>(2)}");
        this.jwtTokenService.GetRefreshTokenExpiry().Returns(_ => this.Now.AddDays(7));
        this.jwtTokenService.RefreshTokenReuseGracePeriod.Returns(GracePeriod);

        this.handler = new RefreshTokenCommandHandler(
            this.userRepository,
            new AuthSessionIssuer(this.userRepository, new FakeEmployeeRepository(), this.jwtTokenService),
            this.jwtTokenService,
            this.time,
            NullLogger<RefreshTokenCommandHandler>.Instance);
    }

    private DateTime Now => this.time.GetUtcNow().UtcDateTime;

    [Fact]
    public async Task Handle_ActiveToken_ExchangesItForANewOneInTheSameSession()
    {
        var presented = this.AddToken("plain");

        var result = await this.RefreshAsync("plain");

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("issued-1");
        result.Value.AccessToken.Should().Be($"access-for-{this.sessionId}");
        presented.RevokedAt.Should().Be(this.Now);

        var successor = this.FindToken("issued-1");
        successor.SessionId.Should().Be(this.sessionId);
        successor.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExchangedTokenPresentedAgainWithinTheGracePeriod_CarriesOnInTheSameSession()
    {
        this.AddToken("plain");
        await this.RefreshAsync("plain");
        this.time.Advance(TimeSpan.FromSeconds(10));

        var result = await this.RefreshAsync("plain");

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("issued-2");
        this.FindToken("issued-2").SessionId.Should().Be(this.sessionId);
        this.FindToken("issued-1").RevokedAt.Should().BeNull("the token the other response carried stays usable");
    }

    [Fact]
    public async Task Handle_ExchangedTokenPresentedAfterTheGracePeriod_SignsTheSessionOut()
    {
        this.AddToken("plain");
        var otherSession = this.AddToken("other-device", Guid.NewGuid());
        await this.RefreshAsync("plain");
        this.time.Advance(TimeSpan.FromMinutes(1));

        var result = await this.RefreshAsync("plain");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.RefreshTokenInvalid);
        this.FindToken("issued-1").RevokedAt.Should().NotBeNull("whoever holds the successor may be the thief");
        otherSession.RevokedAt.Should().BeNull("only the session the token belongs to is compromised");
        (await this.RefreshAsync("issued-1")).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TokenOfASignedOutSession_IsRejectedEvenWithinTheGracePeriod()
    {
        this.AddToken("plain");
        await this.RefreshAsync("plain");
        await this.userRepository.RevokeSessionAsync(this.sessionId, CancellationToken.None);
        this.time.Advance(TimeSpan.FromSeconds(5));

        var result = await this.RefreshAsync("plain");

        result.IsFailure.Should().BeTrue();
        this.userRepository.RefreshTokens.Should().HaveCount(2, "no token may be issued for a session that has ended");
    }

    [Fact]
    public async Task Handle_ConcurrentRefreshWithTheSameTokenWinsTheRace_StillSucceeds()
    {
        this.AddToken("plain");
        this.userRepository.LoseNextRotationRace = true;

        var result = await this.RefreshAsync("plain");

        result.IsSuccess.Should().BeTrue();
        this.FindToken("issued-1").SessionId.Should().Be(this.sessionId);
    }

    [Fact]
    public async Task Handle_ExpiredToken_IsRejected()
    {
        this.AddToken("plain").ExpiresAt = this.Now.AddSeconds(-1);

        var result = await this.RefreshAsync("plain");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.RefreshTokenInvalid);
    }

    [Fact]
    public async Task Handle_InactiveUser_IsRejected()
    {
        this.AddToken("plain");
        this.userRepository.Users[1].IsActive = false;

        var result = await this.RefreshAsync("plain");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.UserInactive);
    }

    [Fact]
    public async Task Handle_UnknownToken_IsRejected()
    {
        var result = await this.RefreshAsync("never-issued");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ErrorCodes.RefreshTokenInvalid);
    }

    [Fact]
    public async Task Handle_KeepsTheRoleTheSessionWorksIn()
    {
        this.userRepository.Users[1] = AuthTestUsers.WithRoles(
            1,
            "user@local.dev",
            RoleCodes.Evaluator,
            RoleCodes.Controller);
        this.AddToken("plain", activeRole: RoleCodes.Controller);

        var result = await this.RefreshAsync("plain");

        result.Value.User.ActiveRole.Should().Be(RoleCodes.Controller);
        this.jwtTokenService.Received(1).GenerateAccessToken(
            Arg.Any<User>(),
            Arg.Is<IEnumerable<string>>(roles => roles.SequenceEqual(new[] { RoleCodes.Controller })),
            Arg.Any<Guid>());
        this.FindToken("issued-1").ActiveRoleCode.Should().Be(RoleCodes.Controller);
    }

    [Fact]
    public async Task Handle_DoesNotOverwriteTheRoleTheAccountLastChose()
    {
        var user = AuthTestUsers.WithRoles(1, "user@local.dev", RoleCodes.Evaluator, RoleCodes.Controller);
        user.LastActiveRoleCode = RoleCodes.Controller;
        this.userRepository.Users[1] = user;
        this.AddToken("plain", activeRole: RoleCodes.Evaluator);

        var result = await this.RefreshAsync("plain");

        result.Value.User.ActiveRole.Should().Be(RoleCodes.Evaluator);
        this.userRepository.Users[1].LastActiveRoleCode.Should().Be(RoleCodes.Controller);
    }

    [Fact]
    public async Task Handle_RoleOfTheSessionWasTakenAway_FallsBackToARoleTheUserHolds()
    {
        this.userRepository.Users[1] = AuthTestUsers.WithRoles(1, "user@local.dev", RoleCodes.Employee);
        this.AddToken("plain", activeRole: RoleCodes.Controller);

        var result = await this.RefreshAsync("plain");

        result.Value.User.ActiveRole.Should().Be(RoleCodes.Employee);
    }

    [Fact]
    public async Task Handle_SessionFromBeforeTheRoleWasCarried_GetsTheLastOrFirstRole()
    {
        this.userRepository.Users[1] = AuthTestUsers.WithRoles(
            1,
            "user@local.dev",
            RoleCodes.Admin,
            RoleCodes.Evaluator);
        this.AddToken("plain", activeRole: null);

        var result = await this.RefreshAsync("plain");

        result.Value.User.ActiveRole.Should().Be(RoleCodes.Evaluator);
    }

    private Task<CSharpFunctionalExtensions.Result<Application.Auth.Models.AuthResponse>> RefreshAsync(string plain) =>
        this.handler.Handle(new RefreshTokenCommand(plain), CancellationToken.None);

    private RefreshToken AddToken(string plain, Guid? sessionId = null, string? activeRole = null)
    {
        var token = new RefreshToken
        {
            UserId = 1,
            SessionId = sessionId ?? this.sessionId,
            TokenHash = LoginCommandHandler.HashToken(plain),
            ExpiresAt = this.Now.AddDays(7),
            ActiveRoleCode = activeRole,
        };
        this.userRepository.RefreshTokens.Add(token);
        return token;
    }

    private RefreshToken FindToken(string plain) =>
        this.userRepository.RefreshTokens.Single(rt => rt.TokenHash == LoginCommandHandler.HashToken(plain));

    /// <summary>
    /// Starts at the real time: the fake repository deletes expired tokens by the real clock.
    /// </summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => this.now;

        public void Advance(TimeSpan by) => this.now += by;
    }
}
