namespace VariableCompensation.Domain.Entities.Evaluation;

public class EvaluationMeasure
{
    public long Id { get; set; }

    public long EvaluationId { get; set; }

    public Evaluation Evaluation { get; set; } = null!;

    public long MeasureTypeId { get; set; }

    public Lookup.MeasureType MeasureType { get; set; } = null!;

    public string? RatingComment { get; set; }

    public long RatingLevelId { get; set; }

    public Lookup.RatingLevel RatingLevel { get; set; } = null!;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
