namespace VariableCompensation.Domain.Entities.Compensation;

public class VariableCompensationResult
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }

    public Hr.Employee Employee { get; set; } = null!;

    public long ParametersId { get; set; }

    public VariableCompensationParameters Parameters { get; set; } = null!;

    public short Year { get; set; }

    public decimal GoalsAverage { get; set; }

    public decimal MeasuresAverage { get; set; }

    public decimal OverallAverage { get; set; }

    public int Points { get; set; }

    public decimal SalaryPointsValue { get; set; }

    public decimal Weight { get; set; }

    public decimal ZScore { get; set; }

    public decimal NormalizedZScore { get; set; }

    public decimal NormalizedPoints { get; set; }

    public decimal CompensationWithoutSalary { get; set; }

    public decimal Aq { get; set; }

    public decimal NetCompensation { get; set; }

    public decimal QuarterlyCompensation { get; set; }

    public decimal MonthlyCompensation { get; set; }

    public decimal CompensationPercent { get; set; }

    public string FormulaVersion { get; set; } = "v1";

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public bool IsFinal { get; set; }

    public long? CreatedByUserId { get; set; }

    public ICollection<VariableCompensationEvaluationLink> EvaluationLinks { get; set; } =
        new List<VariableCompensationEvaluationLink>();
}
