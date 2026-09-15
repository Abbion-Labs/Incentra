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
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal MinAverage { get; init; }

    public decimal MaxAverage { get; init; }

    public int SortOrder { get; init; }

    public decimal RecommendedShare { get; init; }

    public bool IsActive { get; init; } = true;
}
