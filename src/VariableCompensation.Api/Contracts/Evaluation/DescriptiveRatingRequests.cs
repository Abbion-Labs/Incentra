namespace VariableCompensation.Api.Contracts.Evaluation;

public sealed class CreateDescriptiveRatingRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal MinAverage { get; init; }

    public decimal MaxAverage { get; init; }

    public int SortOrder { get; init; }

    public decimal RecommendedShare { get; init; }
}

public sealed class UpdateDescriptiveRatingRequest
{
    /// <summary>The version the edit was made from. Required: an edit from an outdated copy is refused.</summary>
    public int? Version { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal MinAverage { get; init; }

    public decimal MaxAverage { get; init; }

    public int SortOrder { get; init; }

    public decimal RecommendedShare { get; init; }

    public bool IsActive { get; init; } = true;
}
