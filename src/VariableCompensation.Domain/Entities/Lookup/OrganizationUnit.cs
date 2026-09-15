namespace VariableCompensation.Domain.Entities.Lookup;

public class OrganizationUnit
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Hr.Employee> Employees { get; set; } = new List<Hr.Employee>();

    public ICollection<Compensation.VariableCompensationParameters> CompensationParameters { get; set; } =
        new List<Compensation.VariableCompensationParameters>();
}
