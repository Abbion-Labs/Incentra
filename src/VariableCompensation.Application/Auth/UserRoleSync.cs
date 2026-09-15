using CSharpFunctionalExtensions;
using VariableCompensation.Application.Auth.Commands.RegisterUser;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Identity;

namespace VariableCompensation.Application.Auth;

internal static class UserRoleSync
{
    internal static async Task<Result<IReadOnlyList<long>>> ResolveRoleIdsAsync(
        IReadOnlyList<string> roleCodes,
        IRoleLookup roleLookup,
        CancellationToken cancellationToken)
    {
        if (roleCodes.Count == 0)
        {
            return Result.Failure<IReadOnlyList<long>>(ErrorCodes.UserRolesRequired);
        }

        var normalized = roleCodes
            .Select(code => code.Trim().ToUpperInvariant())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
        {
            return Result.Failure<IReadOnlyList<long>>(ErrorCodes.UserRolesRequired);
        }

        var roleIds = new List<long>(normalized.Count);
        foreach (var code in normalized)
        {
            var roleId = await roleLookup.FindRoleIdByCodeAsync(code, cancellationToken);
            if (roleId is null)
            {
                return Result.Failure<IReadOnlyList<long>>(ErrorCodes.RoleDoesNotExist);
            }

            roleIds.Add(roleId.Value);
        }

        return Result.Success<IReadOnlyList<long>>(roleIds);
    }

    internal static void ApplyRoles(User user, IReadOnlyList<long> roleIds)
    {
        var targetRoleIds = roleIds.ToHashSet();
        var assignmentsToRemove = user.UserRoles.Where(ur => !targetRoleIds.Contains(ur.RoleId)).ToList();
        foreach (var assignment in assignmentsToRemove)
        {
            user.UserRoles.Remove(assignment);
        }

        var existingRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        var now = DateTime.UtcNow;
        foreach (var roleId in roleIds)
        {
            if (existingRoleIds.Contains(roleId))
            {
                continue;
            }

            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                AssignedAt = now,
            });
        }
    }
}
