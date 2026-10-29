using VariableCompensation.Application.Auth.Models;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Application.Auth;

public static class UserProfileMapper
{
    public static UserProfileResponse Map(
        User user,
        IReadOnlyList<string> roles,
        Employee? employee = null,
        string? activeRole = null) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            Roles = roles,
            ActiveRole = activeRole,
            EmployeeId = employee?.Id,
            EmployeeFullName = employee?.FullName,
            EmployeeFirstName = employee?.FirstName,
            EmployeeLastName = employee?.LastName,
            EmployeeAvatarUrl = employee?.AvatarUrl,
            EmailNotificationsEnabled = user.EmailNotificationsEnabled
        };
}
