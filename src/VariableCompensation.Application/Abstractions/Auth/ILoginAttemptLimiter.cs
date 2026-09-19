namespace VariableCompensation.Application.Abstractions.Auth;

public interface ILoginAttemptLimiter
{
    TimeSpan? GetRetryAfter(string key);

    void RecordFailure(string key);

    void Reset(string key);
}
