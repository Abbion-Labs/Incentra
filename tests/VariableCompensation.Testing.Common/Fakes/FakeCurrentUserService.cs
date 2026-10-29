using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public long? UserId { get; set; } = 1;

    public Guid? SessionId { get; set; }

    public bool IsAuthenticated { get; set; } = true;

    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>The role of the session: the single role the token carries.</summary>
    public string? ActiveRole => this.Roles.Count == 1 ? this.Roles[0] : null;

    public bool IsAdmin => this.Roles.Contains(RoleCodes.Admin, StringComparer.OrdinalIgnoreCase);

    public bool IsInRole(string role) =>
        this.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public static FakeCurrentUserService AsAdmin(long userId = 1) => new()
    {
        UserId = userId,
        Roles = [RoleCodes.Admin],
    };

    public static FakeCurrentUserService AsEvaluator(long userId = 2, long employeeId = 2) => new()
    {
        UserId = userId,
        Roles = [RoleCodes.Evaluator],
    };

    public static FakeCurrentUserService AsController(long userId = 3) => new()
    {
        UserId = userId,
        Roles = [RoleCodes.Controller],
    };

    public static FakeCurrentUserService AsEmployee(long userId = 4) => new()
    {
        UserId = userId,
        Roles = [RoleCodes.Employee],
    };

    public static FakeCurrentUserService AsUser(long userId) => new()
    {
        UserId = userId,
        Roles = [],
    };
}
