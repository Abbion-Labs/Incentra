namespace VariableCompensation.Domain.Entities.Identity;

public class UserRole
{
    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public long RoleId { get; set; }

    public Lookup.Role Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
