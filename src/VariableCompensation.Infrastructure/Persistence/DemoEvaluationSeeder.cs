using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Infrastructure.Persistence;

internal static class DemoEvaluationSeeder
{
    private sealed record EmployeeProfile(string First, string Last, string OrgCode, string PositionName);

    internal sealed record QuarterPlan(
        short Year,
        byte Quarter,
        EvaluationStatus Status,
        int[] GoalRatings,
        int[] MeasureRatings,
        bool ReturnedForRevision = false);

    private static readonly EmployeeProfile[] SubordinateProfiles =
    {
        new("Marko", "Marković", "FIN", "Saradnik"),
        new("Ana", "Anić", "FIN", "Viši saradnik"),
        new("Petar", "Petrović", "PROD", "Saradnik"),
        new("Jelena", "Jelić", "PROD", "Referent"),
        new("Nikola", "Nikolić", "SALES", "Saradnik"),
        new("Marija", "Marić", "SALES", "Menadžer"),
        new("Stefan", "Stefanović", "DEV", "Saradnik"),
        new("Ivana", "Ivanović", "DEV", "Viši saradnik"),
    };

    public static async Task EnsureAsync(AppDbContext context)
    {
        var evaluator = await context.Employees.FirstOrDefaultAsync(e => e.FirstName == "Jovan" && e.LastName == "Jovanović");
        var controller = await context.Employees.FirstOrDefaultAsync(e => e.FirstName == "Milan" && e.LastName == "Kontrolerović");
        if (evaluator is null || controller is null)
        {
            return;
        }

        await AssignSubordinateProfilesAsync(context, evaluator.Id);

        var ratingLevels = await context.RatingLevels.AsNoTracking().ToListAsync();
        var measureTypes = await context.MeasureTypes.AsNoTracking().OrderBy(m => m.SortOrder).ToListAsync();
        var descriptiveRatings = await context.DescriptiveRatings.AsNoTracking().ToListAsync();
        if (ratingLevels.Count == 0 || measureTypes.Count == 0)
        {
            return;
        }

        var scoring = new EvaluationScoringService();
        var employees = await context.Employees
            .Where(e => e.EvaluatorEmployeeId == evaluator.Id)
            .ToListAsync();

        foreach (var employee in employees)
        {
            var plans = BuildPlansForEmployee(employee.FirstName);
            foreach (var plan in plans)
            {
                var exists = await context.Evaluations.AnyAsync(
                    e => e.EmployeeId == employee.Id && e.Year == plan.Year && e.Quarter == plan.Quarter);
                if (exists)
                {
                    continue;
                }

                var evaluation = CreateEvaluation(
                    employee,
                    evaluator.Id,
                    controller.Id,
                    plan,
                    ratingLevels,
                    measureTypes,
                    scoring,
                    descriptiveRatings);

                context.Evaluations.Add(evaluation);
                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task AssignSubordinateProfilesAsync(AppDbContext context, long evaluatorId)
    {
        var orgUnits = await context.OrganizationUnits.ToDictionaryAsync(o => o.Code!, o => o.Id);
        var positions = await context.JobPositions.ToDictionaryAsync(p => p.Name, p => p.Id);
        var changed = false;

        foreach (var profile in SubordinateProfiles)
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

            if (employee.EvaluatorEmployeeId != evaluatorId)
            {
                employee.EvaluatorEmployeeId = evaluatorId;
                changed = true;
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }
    }

    private static IReadOnlyList<QuarterPlan> BuildPlansForEmployee(string firstName) =>
        firstName switch
        {
            "Marko" =>
            [
                new(2025, 1, EvaluationStatus.Approved, [3, 4], [3, 3, 4, 3, 3, 4]),
                new(2025, 2, EvaluationStatus.Approved, [3, 3, 4], [4, 3, 3, 4, 3, 3]),
                new(2025, 3, EvaluationStatus.Approved, [4, 4, 3], [4, 4, 3, 4, 4, 3]),
                new(2025, 4, EvaluationStatus.Approved, [4, 3, 4], [3, 4, 4, 3, 4, 4]),
                new(2026, 1, EvaluationStatus.Approved, [4, 4, 3], [4, 4, 4, 3, 4, 4]),
                new(2026, 2, EvaluationStatus.Draft, [3, 3], []),
                new(2026, 3, EvaluationStatus.Submitted, [4, 3, 4], [4, 3, 4, 3, 4, 3]),
            ],
            "Ana" =>
            [
                new(2025, 1, EvaluationStatus.Approved, [4, 4], [4, 3, 4, 4, 3, 4]),
                new(2025, 2, EvaluationStatus.Approved, [4, 5, 4], [4, 4, 4, 5, 4, 4]),
                new(2025, 3, EvaluationStatus.Approved, [5, 4, 4], [4, 5, 4, 4, 4, 5]),
                new(2025, 4, EvaluationStatus.Approved, [4, 4, 5], [5, 4, 4, 4, 5, 4]),
                new(2026, 1, EvaluationStatus.Approved, [5, 4, 4], [4, 5, 4, 5, 4, 4]),
                new(2026, 2, EvaluationStatus.UnderReview, [4, 4, 5], [4, 4, 5, 4, 4, 5]),
            ],
            "Petar" =>
            [
                new(2025, 2, EvaluationStatus.Approved, [2, 3], [2, 3, 2, 3, 2, 3]),
                new(2025, 3, EvaluationStatus.Approved, [3, 2, 3], [3, 2, 3, 3, 2, 3]),
                new(2025, 4, EvaluationStatus.Approved, [3, 3, 2], [3, 3, 2, 3, 3, 2]),
                new(2026, 1, EvaluationStatus.Approved, [3, 3, 3], [3, 3, 3, 3, 3, 3]),
                new(2026, 2, EvaluationStatus.Draft, [2, 3], []),
            ],
            "Jelena" =>
            [
                new(2025, 1, EvaluationStatus.Approved, [3, 3, 4], [3, 4, 3, 3, 4, 3]),
                new(2025, 3, EvaluationStatus.Approved, [4, 3, 4], [4, 3, 4, 4, 3, 4]),
                new(2025, 4, EvaluationStatus.Approved, [4, 4, 3], [4, 4, 3, 4, 4, 3]),
                new(2026, 1, EvaluationStatus.Approved, [4, 4, 4], [4, 4, 4, 4, 4, 4]),
                new(2026, 2, EvaluationStatus.Draft, [3, 4, 3], [3, 4, 3, 4, 3, 4], ReturnedForRevision: true),
            ],
            "Nikola" =>
            [
                new(2025, 1, EvaluationStatus.Approved, [3, 4, 3], [3, 4, 3, 3, 4, 3]),
                new(2025, 2, EvaluationStatus.Approved, [4, 3, 4], [4, 3, 4, 3, 4, 3]),
                new(2025, 4, EvaluationStatus.Approved, [4, 4, 3], [4, 4, 3, 4, 3, 4]),
                new(2026, 1, EvaluationStatus.Approved, [4, 3, 4], [4, 3, 4, 4, 3, 4]),
                new(2026, 3, EvaluationStatus.Draft, [3, 3], []),
            ],
            "Marija" =>
            [
                new(2025, 2, EvaluationStatus.Approved, [4, 5, 4], [4, 5, 4, 4, 5, 4]),
                new(2025, 3, EvaluationStatus.Approved, [5, 4, 5], [5, 4, 5, 4, 5, 4]),
                new(2025, 4, EvaluationStatus.Approved, [5, 5, 4], [5, 4, 5, 5, 4, 5]),
                new(2026, 1, EvaluationStatus.Approved, [5, 4, 5], [5, 5, 4, 5, 4, 5]),
                new(2026, 2, EvaluationStatus.Approved, [5, 5, 4], [5, 4, 5, 5, 4, 5]),
            ],
            "Stefan" =>
            [
                new(2025, 1, EvaluationStatus.Approved, [2, 2, 3], [2, 3, 2, 2, 3, 2]),
                new(2025, 3, EvaluationStatus.Approved, [3, 2, 3], [3, 2, 3, 2, 3, 3]),
                new(2025, 4, EvaluationStatus.Approved, [3, 3, 2], [3, 3, 2, 3, 3, 2]),
                new(2026, 1, EvaluationStatus.Approved, [3, 3, 3], [3, 3, 3, 3, 3, 3]),
                new(2026, 2, EvaluationStatus.Submitted, [3, 3, 4], [3, 3, 4, 3, 3, 4]),
            ],
            "Ivana" =>
            [
                new(2025, 2, EvaluationStatus.Approved, [4, 4, 3], [4, 4, 3, 4, 4, 3]),
                new(2025, 3, EvaluationStatus.Approved, [4, 5, 4], [4, 5, 4, 4, 4, 5]),
                new(2025, 4, EvaluationStatus.Approved, [5, 4, 4], [4, 5, 4, 5, 4, 4]),
                new(2026, 1, EvaluationStatus.Approved, [4, 5, 5], [5, 4, 5, 4, 5, 4]),
                new(2026, 2, EvaluationStatus.Approved, [5, 4, 5], [5, 4, 5, 5, 4, 5]),
            ],
            _ => [],
        };

    internal static Evaluation CreateEvaluation(
        Employee employee,
        long evaluatorId,
        long controllerId,
        QuarterPlan plan,
        IReadOnlyList<RatingLevel> ratingLevels,
        IReadOnlyList<MeasureType> measureTypes,
        EvaluationScoringService scoring,
        IReadOnlyList<DescriptiveRating> descriptiveRatings)
    {
        var notRated = ratingLevels.First(r => r.Value == 0);
        var now = DateTime.UtcNow;
        var conversationAt = new DateTime(plan.Year, plan.Quarter * 3, 15, 10, 0, 0, DateTimeKind.Utc);

        var evaluation = new Evaluation
        {
            EmployeeId = employee.Id,
            EvaluatorEmployeeId = evaluatorId,
            ControllerEmployeeId = controllerId,
            Year = plan.Year,
            Quarter = plan.Quarter,
            Status = plan.Status,
            ConversationAt = conversationAt,
            EvaluatorComment = $"Razgovor o ciljevima za Q{plan.Quarter}/{plan.Year}.",
            CreatedAt = conversationAt,
            UpdatedAt = now,
            Version = 1,
        };

        if (plan.Status is EvaluationStatus.Submitted or EvaluationStatus.UnderReview or EvaluationStatus.Approved)
        {
            evaluation.SubmittedAt = conversationAt.AddDays(7);
        }

        if (plan.Status is EvaluationStatus.UnderReview or EvaluationStatus.Approved)
        {
            evaluation.ReviewedAt = conversationAt.AddDays(10);
        }

        if (plan.ReturnedForRevision)
        {
            evaluation.Status = EvaluationStatus.Draft;
            evaluation.ControllerComment = "Potrebno dopuniti merila za saradnju.";
            evaluation.SubmittedAt = conversationAt.AddDays(7);
        }
        else if (plan.Status == EvaluationStatus.Approved)
        {
            evaluation.ApprovedAt = conversationAt.AddDays(14);
            evaluation.ControllerComment = "Odobreno.";
        }

        var goalDescriptions = new[]
        {
            "Povećati produktivnost u okviru tima",
            "Završiti ključne projekte u roku",
            "Unaprediti kvalitet izveštavanja",
            "Smanjiti operativne troškove u okviru odeljenja",
            "Unaprediti saradnju sa internim i eksternim partnerima",
        };

        var goalCount = Math.Max(3, plan.GoalRatings.Length);
        for (var i = 0; i < goalCount; i++)
        {
            var rating = i < plan.GoalRatings.Length ? plan.GoalRatings[i] : 0;
            evaluation.Goals.Add(new EvaluationGoal
            {
                Description = goalDescriptions[i % goalDescriptions.Length],
                RatingLevelId = RatingId(ratingLevels, rating, notRated.Id),
                SortOrder = i,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        if (plan.MeasureRatings.Length > 0)
        {
            for (var i = 0; i < measureTypes.Count; i++)
            {
                var rating = plan.MeasureRatings[Math.Min(i, plan.MeasureRatings.Length - 1)];
                evaluation.Measures.Add(new EvaluationMeasure
                {
                    MeasureTypeId = measureTypes[i].Id,
                    RatingLevelId = RatingId(ratingLevels, rating, notRated.Id),
                    SortOrder = i,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }
        else if (plan.GoalRatings.Length > 0)
        {
            foreach (var goal in evaluation.Goals)
            {
                goal.RatingLevelId = notRated.Id;
            }
        }

        var conditionDescriptions = new[]
        {
            "Raspolaživost potrebnim resursima",
            "Stabilnost organizacionih promena u periodu",
            "Podrška rukovodstva u realizaciji ciljeva",
        };

        for (var i = 0; i < conditionDescriptions.Length; i++)
        {
            evaluation.Conditions.Add(new EvaluationCondition
            {
                Description = conditionDescriptions[i],
                SortOrder = i,
                CreatedAt = now,
            });
        }

        var criterionDescriptions = new[]
        {
            "Ostvarenje dogovorenih rokova",
            "Kvalitet i tačnost isporučenih rezultata",
            "Poštovanje internih procedura i standarda",
        };

        for (var i = 0; i < criterionDescriptions.Length; i++)
        {
            evaluation.Criteria.Add(new EvaluationCriterion
            {
                Description = criterionDescriptions[i],
                SortOrder = i,
                CreatedAt = now,
            });
        }

        scoring.Recalculate(evaluation, ratingLevels, descriptiveRatings);

        evaluation.StatusHistory.Add(new EvaluationStatusHistory
        {
            FromStatus = null,
            ToStatus = EvaluationStatus.Draft.ToString(),
            ChangedByUserId = 1,
            ChangedAt = conversationAt,
        });

        var targetStatus = plan.ReturnedForRevision ? EvaluationStatus.Draft : plan.Status;

        if (targetStatus != EvaluationStatus.Draft || plan.MeasureRatings.Length > 0)
        {
            if (targetStatus != EvaluationStatus.Draft)
            {
                evaluation.StatusHistory.Add(new EvaluationStatusHistory
                {
                    FromStatus = EvaluationStatus.Draft.ToString(),
                    ToStatus = targetStatus.ToString(),
                    ChangedByUserId = 1,
                    ChangedAt = evaluation.SubmittedAt ?? now,
                });
            }
        }

        return evaluation;
    }

    private static long RatingId(IReadOnlyList<RatingLevel> ratingLevels, int value, long notRatedId) =>
        ratingLevels.FirstOrDefault(r => r.Value == value)?.Id ?? notRatedId;
}
