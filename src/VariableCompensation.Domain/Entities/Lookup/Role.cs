namespace VariableCompensation.Domain.Entities.Lookup;

public class Role
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Identity.UserRole> UserRoles { get; set; } = new List<Identity.UserRole>();
}
