using Microsoft.EntityFrameworkCore;
using VariableCompensation.Domain.Common;
using VariableCompensation.Domain.Entities.Audit;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Entities.Lookup;
namespace VariableCompensation.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<EducationLevel> EducationLevels => Set<EducationLevel>();

    public DbSet<JobPosition> JobPositions => Set<JobPosition>();

    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();

    public DbSet<RatingLevel> RatingLevels => Set<RatingLevel>();

    public DbSet<DescriptiveRating> DescriptiveRatings => Set<DescriptiveRating>();

    public DbSet<MeasureType> MeasureTypes => Set<MeasureType>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeSalary> EmployeeSalaries => Set<EmployeeSalary>();

    public DbSet<EvaluatorSettings> EvaluatorSettings => Set<EvaluatorSettings>();

    public DbSet<Evaluation> Evaluations => Set<Evaluation>();

    public DbSet<EvaluationGoal> EvaluationGoals => Set<EvaluationGoal>();

    public DbSet<EvaluationCriterion> EvaluationCriteria => Set<EvaluationCriterion>();

    public DbSet<EvaluationMeasure> EvaluationMeasures => Set<EvaluationMeasure>();

    public DbSet<EvaluationTraining> EvaluationTrainings => Set<EvaluationTraining>();

    public DbSet<EvaluationCondition> EvaluationConditions => Set<EvaluationCondition>();

    public DbSet<EvaluationStatusHistory> EvaluationStatusHistories => Set<EvaluationStatusHistory>();

    public DbSet<VariableCompensationParameters> VariableCompensationParameters => Set<VariableCompensationParameters>();

    public DbSet<VariableCompensationResult> VariableCompensationResults => Set<VariableCompensationResult>();

    public DbSet<VariableCompensationEvaluationLink> VariableCompensationEvaluationLinks =>
        Set<VariableCompensationEvaluationLink>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Every versioned record takes part in optimistic concurrency: the update only succeeds when the row still
        // has the version it was read with, so two edits racing each other cannot both get through.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(IVersioned).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(IVersioned.Version))
                .IsConcurrencyToken();
        }

        base.OnModelCreating(modelBuilder);
    }
}
