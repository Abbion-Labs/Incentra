namespace VariableCompensation.Domain.Entities.Audit;

public class AuditLog
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public long EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
