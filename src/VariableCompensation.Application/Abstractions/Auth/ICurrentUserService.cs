namespace VariableCompensation.Application.Abstractions.Auth;

public interface ICurrentUserService
{
    long? UserId { get; }

    /// <summary>
    /// The sign-in the current access token was issued for.
    /// </summary>
    Guid? SessionId { get; }

    bool IsAuthenticated { get; }

    IReadOnlyList<string> Roles { get; }

    bool IsInRole(string role);

    bool IsAdmin { get; }
}

public interface ICurrentEmployeeContext
{
    Task<long?> GetEmployeeIdAsync(CancellationToken cancellationToken);

    Task<bool> HasLinkedEmployeeAsync(CancellationToken cancellationToken);
}
