namespace VariableCompensation.Domain.Entities.Hr;

public class EmployeeSalary
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int Points { get; set; }

    public byte[] EncryptedSalaryPerPoint { get; set; } = [];

    public int KeyVersion { get; set; } = 1;

    public string Currency { get; set; } = "RSD";

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public long? CreatedByUserId { get; set; }

    public long? UpdatedByUserId { get; set; }
}
