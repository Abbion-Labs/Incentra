using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Auth.Commands.RegisterUser;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Infrastructure.Persistence;

namespace VariableCompensation.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository, IRoleLookup
{
    private readonly AppDbContext context;

    public UserRepository(AppDbContext context)
    {
        this.context = context;
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        this.context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> FindByIdWithRolesAsync(long id, CancellationToken cancellationToken) =>
        this.context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllWithRolesAsync(CancellationToken cancellationToken) =>
        await this.context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

    public Task<bool> EmailExistsAsync(string email, long? excludeUserId, CancellationToken cancellationToken) =>
        this.context.Users.AnyAsync(
            u => u.Email == email && (excludeUserId == null || u.Id != excludeUserId),
            cancellationToken);

    public Task<User?> FindByIdForUpdateAsync(long id, CancellationToken cancellationToken) =>
        this.context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> FindByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken)
    {
        var userId = await this.context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => e.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            return null;
        }

        return await this.FindByIdWithRolesAsync(userId.Value, cancellationToken);
    }

    public Task<bool> HasOtherActiveUserInRoleAsync(string roleCode, long excludeUserId, CancellationToken cancellationToken) =>
        this.context.Users.AnyAsync(
            u => u.Id != excludeUserId && u.IsActive && u.UserRoles.Any(ur => ur.Role.Code == roleCode),
            cancellationToken);

    public Task RevokeAllRefreshTokensAsync(long userId, CancellationToken cancellationToken) =>
        this.RevokeAsync(this.context.RefreshTokens.Where(rt => rt.UserId == userId), cancellationToken);

    public Task RevokeOtherSessionsAsync(long userId, Guid keptSessionId, CancellationToken cancellationToken) =>
        this.RevokeAsync(
            this.context.RefreshTokens.Where(rt => rt.UserId == userId && rt.SessionId != keptSessionId),
            cancellationToken);

    public Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
        this.RevokeAsync(this.context.RefreshTokens.Where(rt => rt.SessionId == sessionId), cancellationToken);

    public Task<bool> IsSessionActiveAsync(long userId, Guid sessionId, DateTime now, CancellationToken cancellationToken) =>
        this.context.RefreshTokens.AnyAsync(
            rt => rt.UserId == userId
                && rt.SessionId == sessionId
                && rt.RevokedAt == null
                && rt.ExpiresAt > now
                && rt.User.IsActive,
            cancellationToken);

    public async Task<bool> TryRotateRefreshTokenAsync(
        RefreshToken current,
        RefreshToken successor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await this.context.Database.BeginTransactionAsync(cancellationToken);

        // Conditional, so two refreshes racing with one token cannot both redeem it: the later update waits
        // for the earlier transaction to commit, then no longer matches the row.
        var revoked = await this.context.RefreshTokens
            .Where(rt => rt.Id == current.Id && rt.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.RevokedAt, now), cancellationToken);
        if (revoked == 0)
        {
            return false;
        }

        // Keeps the tracked entity in step with the row without scheduling a second update for it.
        current.RevokedAt = now;
        this.context.Entry(current).State = EntityState.Unchanged;

        await this.context.RefreshTokens.AddAsync(successor, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        this.context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    public Task DeleteExpiredRefreshTokensAsync(long userId, CancellationToken cancellationToken) =>
        this.context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task AddUserAsync(User user, CancellationToken cancellationToken) =>
        await this.context.Users.AddAsync(user, cancellationToken);

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken) =>
        await this.context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);

    public async Task<long?> FindRoleIdByCodeAsync(string roleCode, CancellationToken cancellationToken)
    {
        var role = await this.context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Code == roleCode, cancellationToken);
        return role?.Id;
    }

    private async Task RevokeAsync(IQueryable<RefreshToken> tokens, CancellationToken cancellationToken)
    {
        var active = await tokens.Where(rt => rt.RevokedAt == null).ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in active)
        {
            token.RevokedAt = now;
        }
    }
}
