namespace VariableCompensation.Application.Abstractions.Persistence;

public interface IAuditLogWriter
{
    Task WriteAsync(
        string entityType,
        long entityId,
        string action,
        string? oldValues,
        string? newValues,
        CancellationToken cancellationToken);
}
