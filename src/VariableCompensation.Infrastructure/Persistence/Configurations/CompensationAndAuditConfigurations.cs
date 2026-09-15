using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VariableCompensation.Domain.Entities.Audit;
using VariableCompensation.Domain.Entities.Compensation;
namespace VariableCompensation.Infrastructure.Persistence.Configurations;

internal sealed class VariableCompensationParametersConfiguration : IEntityTypeConfiguration<VariableCompensationParameters>
{
    public void Configure(EntityTypeBuilder<VariableCompensationParameters> builder)
    {
        builder.ToTable("variable_compensation_parameters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MonetaryPool).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.AcceptablePerformanceRating).HasPrecision(4, 2);
        builder.Property(x => x.UpperLimitCoefficient).HasPrecision(10, 4);
        builder.Property(x => x.DependencyWeight).HasPrecision(10, 4);
        builder.Property(x => x.Exponent).HasPrecision(10, 4);
        builder.Property(x => x.AllowNegativeVariable).HasDefaultValue(false);
        builder.HasIndex(x => new { x.OrganizationUnitId, x.Year }).IsUnique();
        builder.HasOne(x => x.OrganizationUnit).WithMany(x => x.CompensationParameters).HasForeignKey(x => x.OrganizationUnitId);
    }
}

internal sealed class VariableCompensationResultConfiguration : IEntityTypeConfiguration<VariableCompensationResult>
{
    public void Configure(EntityTypeBuilder<VariableCompensationResult> builder)
    {
        builder.ToTable("variable_compensation_results");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GoalsAverage).HasPrecision(5, 4);
        builder.Property(x => x.MeasuresAverage).HasPrecision(5, 4);
        builder.Property(x => x.OverallAverage).HasPrecision(5, 4);
        builder.Property(x => x.SalaryPointsValue).HasPrecision(18, 2);
        builder.Property(x => x.Weight).HasPrecision(18, 6);
        builder.Property(x => x.ZScore).HasPrecision(18, 6);
        builder.Property(x => x.NormalizedZScore).HasPrecision(18, 6);
        builder.Property(x => x.NormalizedPoints).HasPrecision(18, 6);
        builder.Property(x => x.CompensationWithoutSalary).HasPrecision(18, 2);
        builder.Property(x => x.Aq).HasPrecision(18, 6);
        builder.Property(x => x.NetCompensation).HasPrecision(18, 2);
        builder.Property(x => x.QuarterlyCompensation).HasPrecision(18, 2);
        builder.Property(x => x.MonthlyCompensation).HasPrecision(18, 2);
        builder.Property(x => x.CompensationPercent).HasPrecision(10, 6);
        builder.Property(x => x.FormulaVersion).HasMaxLength(20);
        builder.HasIndex(x => new { x.EmployeeId, x.ParametersId, x.Year }).IsUnique();
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId);
        builder.HasOne(x => x.Parameters).WithMany(x => x.Results).HasForeignKey(x => x.ParametersId);
    }
}

internal sealed class VariableCompensationEvaluationLinkConfiguration : IEntityTypeConfiguration<VariableCompensationEvaluationLink>
{
    public void Configure(EntityTypeBuilder<VariableCompensationEvaluationLink> builder)
    {
        builder.ToTable("variable_compensation_evaluation_links");
        builder.HasKey(x => new { x.ResultId, x.EvaluationId });
        builder.HasOne(x => x.Result).WithMany(x => x.EvaluationLinks).HasForeignKey(x => x.ResultId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Evaluation).WithMany().HasForeignKey(x => x.EvaluationId);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OldValues).HasColumnType("jsonb");
        builder.Property(x => x.NewValues).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.CreatedAt);
    }
}
