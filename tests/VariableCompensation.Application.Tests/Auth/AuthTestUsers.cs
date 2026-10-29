using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Application.Tests.Auth;

internal static class AuthTestUsers
{
    public static User WithRoles(long id, string email, params string[] roleCodes)
    {
        var user = new User
        {
            Id = id,
            Email = email,
            PasswordHash = "hash",
            IsActive = true,
        };

        foreach (var (code, index) in roleCodes.Select((code, index) => (code, index)))
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = id,
                RoleId = index + 1,
                Role = new Role { Id = index + 1, Code = code, Name = code },
            });
        }

        return user;
    }
}
