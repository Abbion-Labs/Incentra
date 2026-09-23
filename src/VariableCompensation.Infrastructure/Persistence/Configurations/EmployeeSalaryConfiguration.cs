using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeSalaryConfiguration : IEntityTypeConfiguration<EmployeeSalary>
{
    public void Configure(EntityTypeBuilder<EmployeeSalary> builder)
    {
        builder.ToTable("employee_salaries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Points).IsRequired();
        builder.Property(x => x.EncryptedSalaryPerPoint).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        // One current salary per employee, even when two people enter the first one at the same moment.
        builder.HasIndex(x => x.EmployeeId)
            .IsUnique()
            .HasFilter("\"EffectiveTo\" IS NULL")
            .HasDatabaseName("IX_employee_salaries_EmployeeId_current");
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom });
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
