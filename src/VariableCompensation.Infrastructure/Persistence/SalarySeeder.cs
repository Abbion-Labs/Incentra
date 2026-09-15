using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Infrastructure.Persistence;

namespace VariableCompensation.Infrastructure.Persistence;

internal static class SalarySeeder
{
    private const decimal SalaryPerPoint = 1000m;

    private sealed record SalaryPeriod(int Points, decimal SalaryPerPoint, DateOnly From, DateOnly? To);

    public static async Task EnsureDemoSalariesAsync(AppDbContext context, ISensitiveDataEncryptionService encryption)
    {
        if (!await context.EmployeeSalaries.AnyAsync())
        {
            await SeedAllSalariesAsync(context, encryption);
            return;
        }

        await SyncCurrentSalariesAsync(context, encryption);
    }

    public static async Task SyncCurrentSalariesAsync(AppDbContext context, ISensitiveDataEncryptionService encryption)
    {
        var employees = await context.Employees
            .Include(e => e.JobPosition)
            .Where(e => e.IsActive)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var changed = false;

        foreach (var employee in employees)
        {
            var expectedPoints = employee.JobPosition.SortOrder * 10;
            var current = await context.EmployeeSalaries
                .Where(s => s.EmployeeId == employee.Id && s.EffectiveFrom <= today && (s.EffectiveTo == null || s.EffectiveTo >= today))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (current is null)
            {
                context.EmployeeSalaries.Add(new EmployeeSalary
                {
                    EmployeeId = employee.Id,
                    Points = expectedPoints,
                    EncryptedSalaryPerPoint = encryption.EncryptDecimal(SalaryPerPoint),
                    Currency = "RSD",
                    EffectiveFrom = new DateOnly(2026, 1, 1),
                    EffectiveTo = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
                changed = true;
                continue;
            }

            if (current.Points == expectedPoints)
            {
                continue;
            }

            if (current.EffectiveFrom < new DateOnly(2026, 1, 1))
            {
                current.EffectiveTo = new DateOnly(2025, 12, 31);
                context.EmployeeSalaries.Add(new EmployeeSalary
                {
                    EmployeeId = employee.Id,
                    Points = expectedPoints,
                    EncryptedSalaryPerPoint = encryption.EncryptDecimal(SalaryPerPoint),
                    Currency = "RSD",
                    EffectiveFrom = new DateOnly(2026, 1, 1),
                    EffectiveTo = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
            else
            {
                current.Points = expectedPoints;
                current.EncryptedSalaryPerPoint = encryption.EncryptDecimal(SalaryPerPoint);
                current.UpdatedAt = DateTime.UtcNow;
            }

            changed = true;
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedAllSalariesAsync(AppDbContext context, ISensitiveDataEncryptionService encryption)
    {
        var employees = await context.Employees
            .Include(e => e.JobPosition)
            .Where(e => e.IsActive)
            .ToListAsync();

        foreach (var employee in employees)
        {
            var points = employee.JobPosition.SortOrder * 10;
            foreach (var period in BuildPeriodsForPoints(points))
            {
                context.EmployeeSalaries.Add(new EmployeeSalary
                {
                    EmployeeId = employee.Id,
                    Points = period.Points,
                    EncryptedSalaryPerPoint = encryption.EncryptDecimal(period.SalaryPerPoint),
                    Currency = "RSD",
                    EffectiveFrom = period.From,
                    EffectiveTo = period.To,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static IReadOnlyList<SalaryPeriod> BuildPeriodsForPoints(int points) =>
    [
        new SalaryPeriod(points, SalaryPerPoint, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
        new SalaryPeriod(points, SalaryPerPoint, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
        new SalaryPeriod(points, SalaryPerPoint, new DateOnly(2026, 1, 1), null),
    ];
}
