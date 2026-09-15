using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Entities.Audit;
using VariableCompensation.Infrastructure.Persistence;

namespace VariableCompensation.Infrastructure.Audit;

public sealed class AuditLogWriter : IAuditLogWriter
{
    private readonly AppDbContext context;
    private readonly ICurrentUserService currentUserService;
    private readonly IHttpContextAccessor httpContextAccessor;

    public AuditLogWriter(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        this.context = context;
        this.currentUserService = currentUserService;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task WriteAsync(
        string entityType,
        long entityId,
        string action,
        string? oldValues,
        string? newValues,
        CancellationToken cancellationToken)
    {
        var entry = new AuditLog
        {
            UserId = this.currentUserService.UserId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = this.httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow,
        };

        await this.context.AuditLogs.AddAsync(entry, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);
    }
}
