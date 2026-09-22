using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Auth.Commands.Login;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Infrastructure.Auth;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class LoginCommandHandlerTests
{
    private readonly FakeUserRepository userRepository = new();
    private readonly IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly LoginCommandHandler handler;

    public LoginCommandHandlerTests()
    {
        this.userRepository.Users[1] = new User
        {
            Id = 1,
            Email = "user@local.dev",
            PasswordHash = "hash",
            IsActive = true,
        };
        this.passwordHasher.Verify("correct", "hash").Returns(true);
        this.jwtTokenService.GenerateAccessToken(Arg.Any<User>(), Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>()).Returns("access");
        this.jwtTokenService.GenerateRefreshToken().Returns("refresh");

        var limiter = new InMemoryLoginAttemptLimiter(
            Options.Create(new LoginRateLimitOptions { MaxFailedAttempts = 5, WindowSeconds = 60 }),
            TimeProvider.System);

        this.handler = new LoginCommandHandler(
            this.userRepository,
            new FakeEmployeeRepository(),
            this.passwordHasher,
            this.jwtTokenService,
            limiter);
    }

    [Fact]
    public async Task Handle_AfterTooManyFailures_ReturnsTooManyAttemptsWithoutCheckingPassword()
    {
        for (var i = 0; i < 5; i++)
        {
            var failed = await this.handler.Handle(new LoginCommand("user@local.dev", "wrong"), CancellationToken.None);
            failed.Error.Should().Be(ErrorCodes.InvalidEmailOrPassword);
        }

        this.passwordHasher.ClearReceivedCalls();

        var result = await this.handler.Handle(new LoginCommand("user@local.dev", "correct"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith($"{ErrorCodes.TooManyLoginAttempts}?seconds=");
        this.passwordHasher.DidNotReceiveWithAnyArgs().Verify(default!, default!);
    }

    [Fact]
    public async Task Handle_EmailCaseAndWhitespace_AreCountedTogether()
    {
        string[] variants = ["user@local.dev", "USER@local.dev", " user@local.dev ", "User@Local.Dev", "user@LOCAL.dev"];
        foreach (var email in variants)
        {
            await this.handler.Handle(new LoginCommand(email, "wrong"), CancellationToken.None);
        }

        var result = await this.handler.Handle(new LoginCommand("user@local.dev", "correct"), CancellationToken.None);

        result.Error.Should().StartWith(ErrorCodes.TooManyLoginAttempts);
    }

    [Fact]
    public async Task Handle_UnknownEmail_IsAlsoLimited()
    {
        for (var i = 0; i < 5; i++)
        {
            await this.handler.Handle(new LoginCommand("nobody@local.dev", "x"), CancellationToken.None);
        }

        var result = await this.handler.Handle(new LoginCommand("nobody@local.dev", "x"), CancellationToken.None);

        result.Error.Should().StartWith(ErrorCodes.TooManyLoginAttempts);
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ResetsFailureCount()
    {
        for (var i = 0; i < 4; i++)
        {
            await this.handler.Handle(new LoginCommand("user@local.dev", "wrong"), CancellationToken.None);
        }

        var success = await this.handler.Handle(new LoginCommand("user@local.dev", "correct"), CancellationToken.None);
        success.IsSuccess.Should().BeTrue();

        for (var i = 0; i < 4; i++)
        {
            var failed = await this.handler.Handle(new LoginCommand("user@local.dev", "wrong"), CancellationToken.None);
            failed.Error.Should().Be(ErrorCodes.InvalidEmailOrPassword);
        }
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_DeletesOnlyExpiredTokensOfThatUser()
    {
        var expired = new RefreshToken { UserId = 1, TokenHash = "expired", ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        var active = new RefreshToken { UserId = 1, TokenHash = "active", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var otherUsersExpired = new RefreshToken { UserId = 2, TokenHash = "other", ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        this.userRepository.RefreshTokens.AddRange([expired, active, otherUsersExpired]);

        var result = await this.handler.Handle(new LoginCommand("user@local.dev", "correct"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        this.userRepository.RefreshTokens.Should().NotContain(expired);
        this.userRepository.RefreshTokens.Should().Contain([active, otherUsersExpired]);
    }
}
