namespace VariableCompensation.Api.Contracts.Compensation;

public sealed class CreateCompensationParametersRequest
{
    public long OrganizationUnitId { get; init; }

    public short Year { get; init; }

    public decimal MonetaryPool { get; init; }

    public string Currency { get; init; } = "RSD";

    public decimal AcceptablePerformanceRating { get; init; }

    public decimal DependencyWeight { get; init; }

    public decimal Exponent { get; init; }

    public bool AllowNegativeVariable { get; init; }
}

public sealed class UpdateCompensationParametersRequest
{
    public decimal MonetaryPool { get; init; }

    public string Currency { get; init; } = "RSD";

    public decimal AcceptablePerformanceRating { get; init; }

    public decimal DependencyWeight { get; init; }

    public decimal Exponent { get; init; }

    public bool AllowNegativeVariable { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class CalculateCompensationRequest
{
    public bool IsFinal { get; init; }

    public bool RequireAllQuarters { get; init; }

    public bool? AllowNegativeVariable { get; init; }
}
