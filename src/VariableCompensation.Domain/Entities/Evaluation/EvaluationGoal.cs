namespace VariableCompensation.Domain.Entities.Evaluation;

public class EvaluationGoal
{
    public long Id { get; set; }

    public long EvaluationId { get; set; }

    public Evaluation Evaluation { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public long RatingLevelId { get; set; }

    public Lookup.RatingLevel RatingLevel { get; set; } = null!;

    public string? Comment { get; set; }

    public decimal? Weight { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
