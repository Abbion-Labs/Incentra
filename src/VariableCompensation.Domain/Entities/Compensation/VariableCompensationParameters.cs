namespace VariableCompensation.Domain.Entities.Compensation;

public class VariableCompensationParameters
{
    public long Id { get; set; }

    public long OrganizationUnitId { get; set; }

    public Lookup.OrganizationUnit OrganizationUnit { get; set; } = null!;

    public short Year { get; set; }

    public decimal MonetaryPool { get; set; }

    public string Currency { get; set; } = "RSD";

    public decimal AcceptablePerformanceRating { get; set; } = 2.5m;

    public decimal UpperLimitCoefficient { get; set; }

    public decimal DependencyWeight { get; set; }

    public decimal Exponent { get; set; }

    public bool AllowNegativeVariable { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public long? CreatedByUserId { get; set; }

    public ICollection<VariableCompensationResult> Results { get; set; } = new List<VariableCompensationResult>();
}
