using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Infrastructure.Auth;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public long? UserId => SessionClaims.ReadUserId(this.httpContextAccessor.HttpContext?.User);

    public Guid? SessionId => SessionClaims.ReadSessionId(this.httpContextAccessor.HttpContext?.User);

    public bool IsAuthenticated => this.httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public IReadOnlyList<string> Roles =>
        this.httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList()
        ?? [];

    public string? ActiveRole => this.Roles.Count == 1 ? this.Roles[0] : null;

    public bool IsInRole(string role) => this.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool IsAdmin => this.IsInRole(RoleCodes.Admin);
}

public sealed class CurrentEmployeeContext : ICurrentEmployeeContext
{
    private readonly ICurrentUserService currentUserService;
    private readonly IEmployeeRepository employeeRepository;
    private long? cachedEmployeeId;
    private bool resolved;

    public CurrentEmployeeContext(ICurrentUserService currentUserService, IEmployeeRepository employeeRepository)
    {
        this.currentUserService = currentUserService;
        this.employeeRepository = employeeRepository;
    }

    public async Task<long?> GetEmployeeIdAsync(CancellationToken cancellationToken)
    {
        if (this.resolved)
        {
            return this.cachedEmployeeId;
        }

        this.resolved = true;
        if (this.currentUserService.UserId is null)
        {
            return null;
        }

        var employee = await this.employeeRepository.FindByUserIdAsync(this.currentUserService.UserId.Value, cancellationToken);
        this.cachedEmployeeId = employee?.Id;
        return this.cachedEmployeeId;
    }

    public async Task<bool> HasLinkedEmployeeAsync(CancellationToken cancellationToken) =>
        await this.GetEmployeeIdAsync(cancellationToken) is not null;
}
