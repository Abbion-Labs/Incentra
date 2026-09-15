namespace VariableCompensation.Domain.Entities.Lookup;

public class RatingLevel
{
    public long Id { get; set; }

    public int Value { get; set; }

    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
