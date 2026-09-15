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

    public async Task RevokeAllRefreshTokensAsync(long userId, CancellationToken cancellationToken)
    {
        var tokens = await this.context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }
    }

    public Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        this.context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

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
}
