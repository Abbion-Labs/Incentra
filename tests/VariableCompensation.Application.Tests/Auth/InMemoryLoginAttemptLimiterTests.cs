using FluentAssertions;
using Microsoft.Extensions.Options;
using VariableCompensation.Infrastructure.Auth;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class InMemoryLoginAttemptLimiterTests
{
    private readonly ManualTimeProvider time = new();

    [Fact]
    public void GetRetryAfter_BelowLimit_ReturnsNull()
    {
        var limiter = this.CreateLimiter();

        for (var i = 0; i < 4; i++)
        {
            limiter.RecordFailure("user@local.dev");
        }

        limiter.GetRetryAfter("user@local.dev").Should().BeNull();
    }

    [Fact]
    public void GetRetryAfter_AtLimit_ReturnsRemainingWindow()
    {
        var limiter = this.CreateLimiter();

        for (var i = 0; i < 5; i++)
        {
            limiter.RecordFailure("user@local.dev");
        }

        this.time.Advance(TimeSpan.FromSeconds(20));

        limiter.GetRetryAfter("user@local.dev").Should().Be(TimeSpan.FromSeconds(40));
    }

    [Fact]
    public void GetRetryAfter_AfterWindowElapsed_ReturnsNullAndStartsFresh()
    {
        var limiter = this.CreateLimiter();

        for (var i = 0; i < 5; i++)
        {
            limiter.RecordFailure("user@local.dev");
        }

        this.time.Advance(TimeSpan.FromSeconds(60));

        limiter.GetRetryAfter("user@local.dev").Should().BeNull();

        limiter.RecordFailure("user@local.dev");
        limiter.GetRetryAfter("user@local.dev").Should().BeNull();
    }

    [Fact]
    public void Reset_ClearsFailures()
    {
        var limiter = this.CreateLimiter();

        for (var i = 0; i < 5; i++)
        {
            limiter.RecordFailure("user@local.dev");
        }

        limiter.Reset("user@local.dev");

        limiter.GetRetryAfter("user@local.dev").Should().BeNull();
    }

    [Fact]
    public void Keys_AreCountedIndependently()
    {
        var limiter = this.CreateLimiter();

        for (var i = 0; i < 5; i++)
        {
            limiter.RecordFailure("blocked@local.dev");
        }

        limiter.GetRetryAfter("blocked@local.dev").Should().NotBeNull();
        limiter.GetRetryAfter("other@local.dev").Should().BeNull();
    }

    private InMemoryLoginAttemptLimiter CreateLimiter() =>
        new(Options.Create(new LoginRateLimitOptions { MaxFailedAttempts = 5, WindowSeconds = 60 }), this.time);

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => this.now;

        public void Advance(TimeSpan by) => this.now += by;
    }
}
