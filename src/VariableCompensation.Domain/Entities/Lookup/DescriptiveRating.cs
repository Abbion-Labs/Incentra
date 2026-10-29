using VariableCompensation.Domain.Common;

namespace VariableCompensation.Domain.Entities.Lookup;

public class DescriptiveRating : IVersioned
{
    public long Id { get; set; }

    public int Version { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal? MinAverage { get; set; }

    public decimal? MaxAverage { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Recommended share of subordinates (0–1) for analytics distribution targets.</summary>
    public decimal RecommendedShare { get; set; }
}
