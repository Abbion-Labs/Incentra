namespace VariableCompensation.Domain.Entities.Hr;

public class Employee
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public Identity.User? User { get; set; }

    public long OrganizationUnitId { get; set; }

    public Lookup.OrganizationUnit OrganizationUnit { get; set; } = null!;

    public long JobPositionId { get; set; }

    public Lookup.JobPosition JobPosition { get; set; } = null!;

    public long? EducationLevelId { get; set; }

    public Lookup.EducationLevel? EducationLevel { get; set; }

    public long? EvaluatorEmployeeId { get; set; }

    public Employee? Evaluator { get; set; }

    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateOnly? HiredAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public long? CreatedByUserId { get; set; }

    public long? UpdatedByUserId { get; set; }

    public EvaluatorSettings? EvaluatorSettings { get; set; }

    public ICollection<Evaluation.Evaluation> Evaluations { get; set; } = new List<Evaluation.Evaluation>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
