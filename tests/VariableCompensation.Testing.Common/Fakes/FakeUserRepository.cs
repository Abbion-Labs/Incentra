using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    public Dictionary<long, User> Users { get; } = [];

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(this.Users.Values.FirstOrDefault(u => u.Email == email));

    public Task<User?> FindByIdWithRolesAsync(long id, CancellationToken cancellationToken)
    {
        if (!this.Users.TryGetValue(id, out var user))
        {
            return Task.FromResult<User?>(null);
        }

        EnrichRoles(user);
        return Task.FromResult<User?>(user);
    }

    public Task<IReadOnlyList<User>> GetAllWithRolesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<User>>(this.Users.Values.ToList());

    public Task<bool> EmailExistsAsync(string email, long? excludeUserId, CancellationToken cancellationToken) =>
        Task.FromResult(this.Users.Values.Any(u => u.Email == email && (excludeUserId == null || u.Id != excludeUserId)));

    public Task<User?> FindByIdForUpdateAsync(long id, CancellationToken cancellationToken)
    {
        if (!this.Users.TryGetValue(id, out var user))
        {
            return Task.FromResult<User?>(null);
        }

        EnrichRoles(user);
        return Task.FromResult<User?>(user);
    }

    public Task<User?> FindByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(this.Users.Values.FirstOrDefault(u => u.Employee?.Id == employeeId));

    public List<RefreshToken> RefreshTokens { get; } = [];

    /// <summary>
    /// Makes the next <see cref="TryRotateRefreshTokenAsync"/> behave as if a concurrent refresh had redeemed the
    /// token first.
    /// </summary>
    public bool LoseNextRotationRace { get; set; }

    public Task RevokeAllRefreshTokensAsync(long userId, CancellationToken cancellationToken) =>
        this.RevokeAsync(rt => rt.UserId == userId);

    public Task RevokeOtherSessionsAsync(long userId, Guid keptSessionId, CancellationToken cancellationToken) =>
        this.RevokeAsync(rt => rt.UserId == userId && rt.SessionId != keptSessionId);

    public Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
        this.RevokeAsync(rt => rt.SessionId == sessionId);

    public Task<bool> IsSessionActiveAsync(long userId, Guid sessionId, DateTime now, CancellationToken cancellationToken) =>
        Task.FromResult(
            this.Users.TryGetValue(userId, out var user)
            && user.IsActive
            && this.RefreshTokens.Any(rt =>
                rt.UserId == userId && rt.SessionId == sessionId && rt.RevokedAt == null && rt.ExpiresAt > now));

    public Task<bool> TryRotateRefreshTokenAsync(
        RefreshToken current,
        RefreshToken successor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (this.LoseNextRotationRace)
        {
            this.LoseNextRotationRace = false;
            current.RevokedAt = now;
            this.RefreshTokens.Add(new RefreshToken
            {
                UserId = current.UserId,
                SessionId = current.SessionId,
                TokenHash = $"concurrent-{Guid.NewGuid():N}",
                ExpiresAt = successor.ExpiresAt,
            });
            return Task.FromResult(false);
        }

        if (current.RevokedAt is not null)
        {
            return Task.FromResult(false);
        }

        current.RevokedAt = now;
        this.RefreshTokens.Add(successor);
        return Task.FromResult(true);
    }

    public Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(this.RefreshTokens.FirstOrDefault(rt => rt.TokenHash == tokenHash));

    public Task DeleteExpiredRefreshTokensAsync(long userId, CancellationToken cancellationToken)
    {
        this.RefreshTokens.RemoveAll(rt => rt.UserId == userId && rt.ExpiresAt <= DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        this.Users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        this.RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var user in this.Users.Values)
        {
            EnrichRoles(user);
        }

        return Task.CompletedTask;
    }

    private Task RevokeAsync(Func<RefreshToken, bool> predicate)
    {
        var now = DateTime.UtcNow;
        foreach (var token in this.RefreshTokens.Where(rt => rt.RevokedAt is null && predicate(rt)))
        {
            token.RevokedAt = now;
        }

        return Task.CompletedTask;
    }

    private static void EnrichRoles(User user)
    {
        var roleCodes = new Dictionary<long, string>
        {
            [1] = "ADMIN",
            [2] = "PAYROLL",
            [3] = "EVALUATOR",
            [4] = "CONTROLLER",
            [5] = "EMPLOYEE",
        };

        foreach (var assignment in user.UserRoles)
        {
            if (assignment.Role is null && roleCodes.TryGetValue(assignment.RoleId, out var code))
            {
                assignment.Role = new Domain.Entities.Lookup.Role
                {
                    Id = assignment.RoleId,
                    Code = code,
                    Name = code,
                };
            }
        }
    }
}
