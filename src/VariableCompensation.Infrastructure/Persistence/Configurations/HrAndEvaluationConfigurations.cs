using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);
        builder.HasIndex(x => x.IsActive);
        builder.HasOne(x => x.User).WithOne(x => x.Employee).HasForeignKey<Employee>(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.OrganizationUnit).WithMany(x => x.Employees).HasForeignKey(x => x.OrganizationUnitId);
        builder.HasOne(x => x.JobPosition).WithMany(x => x.Employees).HasForeignKey(x => x.JobPositionId);
        builder.HasOne(x => x.EducationLevel).WithMany(x => x.Employees).HasForeignKey(x => x.EducationLevelId);
        builder.HasOne(x => x.Evaluator).WithMany(x => x.Subordinates).HasForeignKey(x => x.EvaluatorEmployeeId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class EvaluatorSettingsConfiguration : IEntityTypeConfiguration<EvaluatorSettings>
{
    public void Configure(EntityTypeBuilder<EvaluatorSettings> builder)
    {
        builder.ToTable("evaluator_settings");
        builder.HasKey(x => x.EmployeeId);
        builder.HasOne(x => x.Employee).WithOne(x => x.EvaluatorSettings).HasForeignKey<EvaluatorSettings>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Controller).WithMany().HasForeignKey(x => x.ControllerEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> builder)
    {
        builder.ToTable("evaluations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.GoalsAverage).HasPrecision(5, 4);
        builder.Property(x => x.MeasuresAverage).HasPrecision(5, 4);
        builder.Property(x => x.OverallAverage).HasPrecision(5, 4);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.Year, x.Quarter });
        builder.HasIndex(x => new { x.EmployeeId, x.Year, x.Quarter }).IsUnique();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne(x => x.Employee).WithMany(x => x.Evaluations).HasForeignKey(x => x.EmployeeId);
        builder.HasOne(x => x.Evaluator).WithMany().HasForeignKey(x => x.EvaluatorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Controller).WithMany().HasForeignKey(x => x.ControllerEmployeeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.DescriptiveRating).WithMany().HasForeignKey(x => x.DescriptiveRatingId);
    }
}

internal sealed class EvaluationGoalConfiguration : IEntityTypeConfiguration<EvaluationGoal>
{
    public void Configure(EntityTypeBuilder<EvaluationGoal> builder)
    {
        builder.ToTable("evaluation_goals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Weight).HasPrecision(5, 2);
        builder.HasOne(x => x.Evaluation).WithMany(x => x.Goals).HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RatingLevel).WithMany().HasForeignKey(x => x.RatingLevelId);
    }
}

internal sealed class EvaluationCriterionConfiguration : IEntityTypeConfiguration<EvaluationCriterion>
{
    public void Configure(EntityTypeBuilder<EvaluationCriterion> builder)
    {
        builder.ToTable("evaluation_criteria");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.Evaluation).WithMany(x => x.Criteria).HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EvaluationMeasureConfiguration : IEntityTypeConfiguration<EvaluationMeasure>
{
    public void Configure(EntityTypeBuilder<EvaluationMeasure> builder)
    {
        builder.ToTable("evaluation_measures");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Evaluation).WithMany(x => x.Measures).HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.MeasureType).WithMany().HasForeignKey(x => x.MeasureTypeId);
        builder.HasOne(x => x.RatingLevel).WithMany().HasForeignKey(x => x.RatingLevelId);
    }
}

internal sealed class EvaluationTrainingConfiguration : IEntityTypeConfiguration<EvaluationTraining>
{
    public void Configure(EntityTypeBuilder<EvaluationTraining> builder)
    {
        builder.ToTable("evaluation_trainings");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EvaluationId).IsUnique();
        builder.HasOne(x => x.Evaluation).WithOne(x => x.Training).HasForeignKey<EvaluationTraining>(x => x.EvaluationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EvaluationConditionConfiguration : IEntityTypeConfiguration<EvaluationCondition>
{
    public void Configure(EntityTypeBuilder<EvaluationCondition> builder)
    {
        builder.ToTable("evaluation_conditions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.Evaluation).WithMany(x => x.Conditions).HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EvaluationStatusHistoryConfiguration : IEntityTypeConfiguration<EvaluationStatusHistory>
{
    public void Configure(EntityTypeBuilder<EvaluationStatusHistory> builder)
    {
        builder.ToTable("evaluation_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasMaxLength(30);
        builder.Property(x => x.ToStatus).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ChangedByRoleCode).HasMaxLength(50);
        builder.HasOne(x => x.Evaluation).WithMany(x => x.StatusHistory).HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Cascade);
    }
}
