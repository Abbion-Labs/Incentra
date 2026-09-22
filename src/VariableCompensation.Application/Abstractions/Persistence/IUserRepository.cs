using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdWithRolesAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> GetAllWithRolesAsync(CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, long? excludeUserId, CancellationToken cancellationToken);

    Task<User?> FindByIdForUpdateAsync(long id, CancellationToken cancellationToken);

    Task<User?> FindByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken);

    Task RevokeAllRefreshTokensAsync(long userId, CancellationToken cancellationToken);

    /// <summary>
    /// Signs out every session of the user except <paramref name="keptSessionId"/>.
    /// </summary>
    Task RevokeOtherSessionsAsync(long userId, Guid keptSessionId, CancellationToken cancellationToken);

    Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Whether the session has not ended: the user is still active and one of the session's refresh tokens is
    /// neither revoked nor expired.
    /// </summary>
    Task<bool> IsSessionActiveAsync(long userId, Guid sessionId, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes <paramref name="current"/> and stores <paramref name="successor"/> together. Returns false, and
    /// stores nothing, when <paramref name="current"/> has been revoked in the meantime, for example by a
    /// concurrent refresh with the same token.
    /// </summary>
    Task<bool> TryRotateRefreshTokenAsync(
        RefreshToken current,
        RefreshToken successor,
        DateTime now,
        CancellationToken cancellationToken);

    Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task DeleteExpiredRefreshTokensAsync(long userId, CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
