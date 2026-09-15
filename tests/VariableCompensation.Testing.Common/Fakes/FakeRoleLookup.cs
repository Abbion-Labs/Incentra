using VariableCompensation.Application.Auth.Commands.RegisterUser;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeRoleLookup : IRoleLookup
{
    private readonly Dictionary<string, long> roles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ADMIN"] = 1,
        ["PAYROLL"] = 2,
        ["EVALUATOR"] = 3,
        ["CONTROLLER"] = 4,
        ["EMPLOYEE"] = 5,
    };

    public Task<long?> FindRoleIdByCodeAsync(string roleCode, CancellationToken cancellationToken) =>
        Task.FromResult(this.roles.TryGetValue(roleCode, out var id) ? id : (long?)null);
}
