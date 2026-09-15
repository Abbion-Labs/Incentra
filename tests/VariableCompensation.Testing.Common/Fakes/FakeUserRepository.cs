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

    public Task RevokeAllRefreshTokensAsync(long userId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult<RefreshToken?>(null);

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        this.Users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var user in this.Users.Values)
        {
            EnrichRoles(user);
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
