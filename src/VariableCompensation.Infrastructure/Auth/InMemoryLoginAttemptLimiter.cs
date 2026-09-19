using Microsoft.Extensions.Options;
using VariableCompensation.Application.Abstractions.Auth;

namespace VariableCompensation.Infrastructure.Auth;

public sealed class InMemoryLoginAttemptLimiter : ILoginAttemptLimiter
{
    private const int MaxKeyLength = 320;
    private const int PruneEveryFailures = 1000;

    private readonly object gate = new();
    private readonly Dictionary<string, Attempts> entries = [];
    private readonly TimeProvider timeProvider;
    private readonly int maxFailedAttempts;
    private readonly TimeSpan window;
    private int failuresSincePrune;

    public InMemoryLoginAttemptLimiter(IOptions<LoginRateLimitOptions> options, TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
        this.maxFailedAttempts = options.Value.MaxFailedAttempts;
        this.window = TimeSpan.FromSeconds(options.Value.WindowSeconds);
    }

    public TimeSpan? GetRetryAfter(string key)
    {
        key = NormalizeKey(key);
        var now = this.timeProvider.GetUtcNow();

        lock (this.gate)
        {
            if (!this.entries.TryGetValue(key, out var attempts))
            {
                return null;
            }

            var windowEnd = attempts.WindowStart + this.window;
            if (now >= windowEnd)
            {
                this.entries.Remove(key);
                return null;
            }

            return attempts.Count >= this.maxFailedAttempts ? windowEnd - now : null;
        }
    }

    public void RecordFailure(string key)
    {
        key = NormalizeKey(key);
        var now = this.timeProvider.GetUtcNow();

        lock (this.gate)
        {
            if (++this.failuresSincePrune >= PruneEveryFailures)
            {
                this.Prune(now);
            }

            if (this.entries.TryGetValue(key, out var attempts) && now < attempts.WindowStart + this.window)
            {
                this.entries[key] = attempts with { Count = attempts.Count + 1 };
            }
            else
            {
                this.entries[key] = new Attempts(1, now);
            }
        }
    }

    public void Reset(string key)
    {
        key = NormalizeKey(key);

        lock (this.gate)
        {
            this.entries.Remove(key);
        }
    }

    private static string NormalizeKey(string key) =>
        key.Length > MaxKeyLength ? key[..MaxKeyLength] : key;

    private void Prune(DateTimeOffset now)
    {
        this.failuresSincePrune = 0;

        foreach (var expired in this.entries.Where(e => now >= e.Value.WindowStart + this.window).Select(e => e.Key).ToList())
        {
            this.entries.Remove(expired);
        }
    }

    private readonly record struct Attempts(int Count, DateTimeOffset WindowStart);
}
