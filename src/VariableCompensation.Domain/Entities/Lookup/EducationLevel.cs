using VariableCompensation.Domain.Common;

namespace VariableCompensation.Domain.Entities.Lookup;

public class EducationLevel : IVersioned
{
    public long Id { get; set; }

    public int Version { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Hr.Employee> Employees { get; set; } = new List<Hr.Employee>();
}
