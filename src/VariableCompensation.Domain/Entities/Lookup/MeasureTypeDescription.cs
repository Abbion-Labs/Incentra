namespace VariableCompensation.Domain.Entities.Lookup;

public class MeasureTypeDescription
{
    public long Id { get; set; }

    public long MeasureTypeId { get; set; }

    public MeasureType MeasureType { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
