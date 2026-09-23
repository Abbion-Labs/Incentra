namespace VariableCompensation.Domain.Entities.Evaluation;

public class EvaluationStatusHistory
{
    public long Id { get; set; }

    public long EvaluationId { get; set; }

    public Evaluation Evaluation { get; set; } = null!;

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = string.Empty;

    public long ChangedByUserId { get; set; }

    /// <summary>The role the user was working in when they changed the status.</summary>
    public string? ChangedByRoleCode { get; set; }

    public string? Comment { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
