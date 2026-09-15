using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Infrastructure.Persistence;

/// <summary>
/// Priprema realistične demo podatke za kalkulaciju varijabilne kompenzacije (2026).
/// Pokreće se pri svakom startu aplikacije i osvežava podatke ako je potrebno.
/// </summary>
internal static class CompensationDemoDataSeeder
{
    private const short DemoYear = 2026;

    private sealed record EmployeeCompensationProfile(
        string First,
        string Last,
        string OrgCode,
        string PositionName,
        int[] Q1GoalRatings,
        int[] Q2GoalRatings,
        int[] Q3GoalRatings,
        int[] Q4GoalRatings);

    private static readonly EmployeeCompensationProfile[] Profiles =
    [
        new("Marko", "Marković", "FIN", "Saradnik", [3, 4, 3], [4, 3, 4], [3, 4, 3], [4, 4, 3]),
        new("Ana", "Anić", "FIN", "Viši saradnik", [4, 4, 5], [4, 5, 4], [5, 4, 4], [5, 5, 4]),
        new("Petar", "Petrović", "PROD", "Saradnik", [2, 3, 2], [3, 2, 3], [2, 3, 2], [3, 3, 2]),
        new("Jelena", "Jelić", "PROD", "Referent", [3, 4, 3], [4, 3, 4], [3, 4, 4], [4, 3, 4]),
        new("Nikola", "Nikolić", "SALES", "Saradnik", [4, 3, 4], [3, 4, 4], [4, 4, 3], [4, 3, 4]),
        new("Marija", "Marić", "SALES", "Menadžer", [5, 4, 5], [4, 5, 5], [5, 5, 4], [5, 4, 5]),
        new("Stefan", "Stefanović", "DEV", "Saradnik", [2, 2, 3], [2, 3, 2], [3, 2, 2], [2, 3, 2]),
        new("Ivana", "Ivanović", "DEV", "Viši saradnik", [4, 5, 4], [5, 4, 5], [4, 5, 5], [5, 4, 5]),
    ];

    private static readonly Dictionary<string, decimal> MonetaryPoolByOrg = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FIN"] = 1_200_000m,
        ["PROD"] = 2_500_000m,
        ["SALES"] = 3_000_000m,
        ["DEV"] = 4_500_000m,
    };

    public static Task EnsureAsync(AppDbContext context, ISensitiveDataEncryptionService encryption) =>
        Task.CompletedTask;

    private static async Task EnsureHeadquartersOrgAsync(AppDbContext context)
    {
        var hq = await context.OrganizationUnits.FirstOrDefaultAsync(o => o.Code == "HQ");
        if (hq is null)
        {
            hq = new OrganizationUnit
            {
                Name = "Uprava i podrška",
                Code = "HQ",
                IsActive = true,
            };
            context.OrganizationUnits.Add(hq);
            await context.SaveChangesAsync();
        }

        var staffNames = new (string First, string Last)[]
        {
            ("Snežana", "Administratorski"),
            ("Milan", "Kontrolerović"),
            ("Jovan", "Jovanović"),
        };

        var changed = false;
        foreach (var (first, last) in staffNames)
        {
            var employee = await context.Employees.FirstOrDefaultAsync(e => e.FirstName == first && e.LastName == last);
            if (employee is not null && employee.OrganizationUnitId != hq.Id)
            {
                employee.OrganizationUnitId = hq.Id;
                changed = true;
            }
        }

        var orgUnits = await context.OrganizationUnits.ToDictionaryAsync(o => o.Code!, o => o.Id);
        var positions = await context.JobPositions.ToDictionaryAsync(p => p.Name, p => p.Id);
        var evaluator = await context.Employees.FirstAsync(e => e.FirstName == "Jovan" && e.LastName == "Jovanović");

        foreach (var profile in Profiles)
        {
            var employee = await context.Employees.FirstOrDefaultAsync(
                e => e.FirstName == profile.First && e.LastName == profile.Last);
            if (employee is null)
            {
                continue;
            }

            if (orgUnits.TryGetValue(profile.OrgCode, out var orgId) && employee.OrganizationUnitId != orgId)
            {
                employee.OrganizationUnitId = orgId;
                changed = true;
            }

            if (positions.TryGetValue(profile.PositionName, out var positionId) && employee.JobPositionId != positionId)
            {
                employee.JobPositionId = positionId;
                changed = true;
            }

            if (employee.EvaluatorEmployeeId != evaluator.Id)
            {
                employee.EvaluatorEmployeeId = evaluator.Id;
                changed = true;
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureCompensationParametersAsync(AppDbContext context)
    {
        var orgUnits = await context.OrganizationUnits
            .Where(o => o.Code != null && MonetaryPoolByOrg.Keys.Contains(o.Code))
            .ToListAsync();

        foreach (var orgUnit in orgUnits)
        {
            if (orgUnit.Code is null || !MonetaryPoolByOrg.TryGetValue(orgUnit.Code, out var pool))
            {
                continue;
            }

            var existing = await context.VariableCompensationParameters
                .FirstOrDefaultAsync(p => p.OrganizationUnitId == orgUnit.Id && p.Year == DemoYear);

            if (existing is null)
            {
                context.VariableCompensationParameters.Add(new VariableCompensationParameters
                {
                    OrganizationUnitId = orgUnit.Id,
                    Year = DemoYear,
                    MonetaryPool = pool,
                    Currency = "RSD",
                    AcceptablePerformanceRating = 2.5m,
                    UpperLimitCoefficient = 0.25m,
                    DependencyWeight = 1.0m,
                    Exponent = 1.5m,
                    AllowNegativeVariable = false,
                    IsActive = true,
                });
            }
            else
            {
                existing.MonetaryPool = pool;
                existing.Currency = "RSD";
                existing.AcceptablePerformanceRating = 2.5m;
                existing.DependencyWeight = 1.0m;
                existing.Exponent = 1.5m;
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task DeactivateDuplicateEmployeesAsync(AppDbContext context)
    {
        var canonical = await context.Employees.FirstOrDefaultAsync(e => e.FirstName == "Marko" && e.LastName == "Marković");
        var duplicates = await context.Employees
            .Where(e => e.FirstName == "Marko" && e.LastName != "Marković" && e.IsActive)
            .ToListAsync();

        if (canonical is null || duplicates.Count == 0)
        {
            return;
        }

        foreach (var duplicate in duplicates)
        {
            duplicate.IsActive = false;
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureApprovedEvaluations2026Async(
        AppDbContext context,
        long evaluatorId,
        long controllerId)
    {
        var employeeIds = new List<long>();
        foreach (var profile in Profiles)
        {
            var employee = await context.Employees.FirstOrDefaultAsync(
                e => e.FirstName == profile.First && e.LastName == profile.Last);
            if (employee is not null)
            {
                employeeIds.Add(employee.Id);
            }
        }

        if (employeeIds.Count == 0)
        {
            return;
        }

        var existing2026 = await context.Evaluations
            .Where(e => employeeIds.Contains(e.EmployeeId) && e.Year == DemoYear)
            .Select(e => e.Id)
            .ToListAsync();

        if (existing2026.Count > 0)
        {
            await context.EvaluationStatusHistories
                .Where(h => existing2026.Contains(h.EvaluationId))
                .ExecuteDeleteAsync();
            await context.EvaluationGoals.Where(g => existing2026.Contains(g.EvaluationId)).ExecuteDeleteAsync();
            await context.EvaluationMeasures.Where(m => existing2026.Contains(m.EvaluationId)).ExecuteDeleteAsync();
            await context.EvaluationConditions.Where(c => existing2026.Contains(c.EvaluationId)).ExecuteDeleteAsync();
            await context.EvaluationCriteria.Where(c => existing2026.Contains(c.EvaluationId)).ExecuteDeleteAsync();
            await context.EvaluationTrainings.Where(t => existing2026.Contains(t.EvaluationId)).ExecuteDeleteAsync();
            await context.Evaluations.Where(e => existing2026.Contains(e.Id)).ExecuteDeleteAsync();
        }

        var ratingLevels = await context.RatingLevels.AsNoTracking().ToListAsync();
        var measureTypes = await context.MeasureTypes.AsNoTracking().OrderBy(m => m.SortOrder).ToListAsync();
        var descriptiveRatings = await context.DescriptiveRatings.AsNoTracking().ToListAsync();
        var scoring = new EvaluationScoringService();

        foreach (var profile in Profiles)
        {
            var employee = await context.Employees.FirstOrDefaultAsync(
                e => e.FirstName == profile.First && e.LastName == profile.Last);
            if (employee is null)
            {
                continue;
            }

            var quarterGoalRatings = new[] { profile.Q1GoalRatings, profile.Q2GoalRatings, profile.Q3GoalRatings, profile.Q4GoalRatings };
            for (byte quarter = 1; quarter <= 4; quarter++)
            {
                var goalRatings = quarterGoalRatings[quarter - 1];
                var measureRatings = BuildMeasureRatings(goalRatings);

                var plan = new DemoEvaluationSeeder.QuarterPlan(
                    DemoYear,
                    quarter,
                    EvaluationStatus.Approved,
                    goalRatings,
                    measureRatings);

                var evaluation = DemoEvaluationSeeder.CreateEvaluation(
                    employee,
                    evaluatorId,
                    controllerId,
                    plan,
                    ratingLevels,
                    measureTypes,
                    scoring,
                    descriptiveRatings);

                context.Evaluations.Add(evaluation);
            }
        }

        await context.SaveChangesAsync();
    }

    private static int[] BuildMeasureRatings(int[] goalRatings)
    {
        var baseRating = goalRatings.Length > 0 ? (int)Math.Round(goalRatings.Average()) : 3;
        return
        [
            baseRating,
            Math.Min(5, baseRating + 1),
            baseRating,
            Math.Max(1, baseRating - 1),
            baseRating,
            Math.Min(5, baseRating),
        ];
    }
}
