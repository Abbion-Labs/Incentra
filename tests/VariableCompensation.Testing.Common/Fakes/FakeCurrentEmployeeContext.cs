using VariableCompensation.Application.Abstractions.Auth;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeCurrentEmployeeContext : ICurrentEmployeeContext
{
    public long? EmployeeId { get; set; }

    public Task<long?> GetEmployeeIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(this.EmployeeId);

    public Task<bool> HasLinkedEmployeeAsync(CancellationToken cancellationToken) =>
        Task.FromResult(this.EmployeeId.HasValue);
}
