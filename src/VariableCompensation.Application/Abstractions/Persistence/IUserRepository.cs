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

    Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task DeleteExpiredRefreshTokensAsync(long userId, CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
