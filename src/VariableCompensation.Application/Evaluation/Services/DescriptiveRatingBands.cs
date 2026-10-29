using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Application.Evaluation.Services;

/// <summary>
/// Picks the descriptive rating for an average. Each band runs from its own minimum up to the next band's minimum,
/// so an average always lands in exactly one band. Matching on both ends would leave the averages between one
/// band's maximum and the next one's minimum (3.49 and 3.50, say) without a rating, and averages carry four
/// decimals. The maximum is only honoured for the top band.
/// </summary>
public static class DescriptiveRatingBands
{
    public static DescriptiveRating? Resolve(IReadOnlyList<DescriptiveRating> ratings, decimal average)
    {
        var bands = ratings
            .Where(r => r.IsActive && r.MinAverage is not null)
            .OrderBy(r => r.MinAverage)
            .ToList();

        var band = bands.LastOrDefault(r => r.MinAverage <= average);
        if (band is null)
        {
            return null;
        }

        var isTopBand = ReferenceEquals(band, bands[^1]);
        return isTopBand && band.MaxAverage is not null && average > band.MaxAverage ? null : band;
    }
}
