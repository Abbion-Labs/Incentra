using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Infrastructure.Persistence;

/// <summary>
/// Seeds a realistic organization with 120 employees, historical evaluations, and compensation parameters.
/// Runs once when the database does not match the expected realistic seed state.
/// </summary>
internal static class RealisticOrganizationSeeder
{
    public const int SeedVersion = 1;
    public const int ExpectedActiveEmployees = 120;
    public const int ExpectedOrgUnits = 6;

    private const decimal SalaryPerPoint = 1000m;

    private static readonly string[] FixedDemoUserEmails =
    [
        "admin@local.dev",
        "payroll@local.dev",
    ];

    // The first evaluator and controller keep the plain addresses the README
    // documents; the rest are numbered.
    private static string EvaluatorEmail(int index) =>
        index == 0 ? "evaluator@local.dev" : $"evaluator{index + 1}@local.dev";

    private static string ControllerEmail(int index) =>
        index == 0 ? "controller@local.dev" : $"controller{index + 1}@local.dev";

    private static IReadOnlyCollection<string> DemoUserEmails() =>
    [
        .. FixedDemoUserEmails,
        .. Enumerable.Range(0, OrgUnitDefinitions.Length).Select(EvaluatorEmail),
        .. Enumerable.Range(0, OrgUnitDefinitions.Length).Select(ControllerEmail),
    ];

    private static readonly (string Code, string Name, decimal Pool2024, decimal Pool2025)[] OrgUnitDefinitions =
    [
        ("FIN", "Sektor finansija i računovodstva", 1_800_000m, 2_000_000m),
        ("PROD", "Sektor proizvodnje", 3_500_000m, 4_000_000m),
        ("SALES", "Sektor prodaje i marketinga", 4_200_000m, 4_800_000m),
        ("IT", "Sektor informacionih tehnologija", 5_500_000m, 6_200_000m),
        ("HR", "Sektor ljudskih resursa", 1_200_000m, 1_400_000m),
        ("LOG", "Sektor logistike i nabavke", 2_800_000m, 3_100_000m),
    ];

    private static readonly string[] MaleFirstNames =
    [
        "Milan", "Jovan", "Nikola", "Petar", "Marko", "Stefan", "Luka", "Nemanja",
        "Aleksandar", "Dušan", "Igor", "Vladimir", "Zoran", "Dragan", "Miodrag",
        "Saša", "Bojan", "Dejan", "Goran", "Filip", "Miloš", "Uroš", "Vuk",
        "Đorđe", "Predrag", "Radovan", "Slobodan", "Branislav", "Darko", "Nebojša",
    ];

    private static readonly string[] FemaleFirstNames =
    [
        "Ana", "Jelena", "Marija", "Ivana", "Milica", "Tamara", "Snežana", "Olivera",
        "Danijela", "Katarina", "Maja", "Teodora", "Jovana", "Nataša", "Vesna",
        "Dragana", "Gordana", "Ljiljana", "Mirjana", "Sanja", "Biljana", "Jasmina",
        "Aleksandra", "Andrea", "Kristina", "Tijana", "Una", "Zorica", "Radmila", "Božana",
    ];

    private static readonly string[] LastNames =
    [
        "Petrović", "Jovanović", "Nikolić", "Marković", "Đorđević", "Stojanović", "Ilić",
        "Pavlović", "Milošević", "Popović", "Radović", "Kostić", "Filipović", "Lazić",
        "Todorović", "Vuković", "Simić", "Mitić", "Ristić", "Antić", "Janković", "Stanković",
        "Kovačević", "Mladenović", "Obradović", "Đukić", "Božić", "Savić", "Perić", "Vasić",
        "Arsić", "Cvetković", "Gajić", "Jović", "Knežević", "Lukić", "Maksimović", "Nedić",
        "Pantić", "Radojević", "Stefanović", "Tomić", "Urošević", "Vojinović", "Zdravković",
    ];

    private static readonly int[] RatingWeights = [5, 15, 35, 35, 10];

    public static async Task EnsureAsync(AppDbContext context, ISensitiveDataEncryptionService encryption)
    {
        if (!await IsRealisticSeedPresentAsync(context))
        {
            await WipeTransactionalDataAsync(context);
            await SeedOrganizationUnitsAsync(context);
            await SeedEmployeesAsync(context);
            await SeedSalariesAsync(context, encryption);
            await SeedCompensationParametersAsync(context);
            await SeedEvaluationsAsync(context);
        }

        await SeedUsersAsync(context);
    }

    private static async Task<bool> IsRealisticSeedPresentAsync(AppDbContext context)
    {
        var activeEmployees = await context.Employees.CountAsync(e => e.IsActive);
        if (activeEmployees != ExpectedActiveEmployees)
        {
            return false;
        }

        var orgUnits = await context.OrganizationUnits.CountAsync(o => o.IsActive);
        if (orgUnits != ExpectedOrgUnits)
        {
            return false;
        }

        if (await context.OrganizationUnits.AnyAsync(o => o.Code == "HQ"))
        {
            return false;
        }

        if (await context.Users.AnyAsync(u => u.Email == "marko@local.dev"))
        {
            return false;
        }

        if (!await context.Users.AnyAsync(u => u.Email == "payroll@local.dev"))
        {
            return false;
        }

        if (!await context.Users.AnyAsync(u => u.Email == "evaluator@local.dev"))
        {
            return false;
        }

        if (!await context.Users.AnyAsync(u => u.Email == "controller@local.dev"))
        {
            return false;
        }

        var orgUnitIds = await context.OrganizationUnits.Select(o => o.Id).ToListAsync();
        var params2024 = await context.VariableCompensationParameters.CountAsync(
            p => p.Year == 2024 && orgUnitIds.Contains(p.OrganizationUnitId));
        var params2025 = await context.VariableCompensationParameters.CountAsync(
            p => p.Year == 2025 && orgUnitIds.Contains(p.OrganizationUnitId));

        if (params2024 != ExpectedOrgUnits || params2025 != ExpectedOrgUnits)
        {
            return false;
        }

        if (await context.VariableCompensationParameters.AnyAsync(p => p.Year == 2026))
        {
            return false;
        }

        var evaluators = await context.EvaluatorSettings.CountAsync();
        if (evaluators != 6)
        {
            return false;
        }

        return true;
    }

    private static async Task WipeTransactionalDataAsync(AppDbContext context)
    {
        await context.VariableCompensationEvaluationLinks.ExecuteDeleteAsync();
        await context.VariableCompensationResults.ExecuteDeleteAsync();
        await context.EvaluationStatusHistories.ExecuteDeleteAsync();
        await context.EvaluationGoals.ExecuteDeleteAsync();
        await context.EvaluationMeasures.ExecuteDeleteAsync();
        await context.EvaluationConditions.ExecuteDeleteAsync();
        await context.EvaluationCriteria.ExecuteDeleteAsync();
        await context.EvaluationTrainings.ExecuteDeleteAsync();
        await context.Evaluations.ExecuteDeleteAsync();
        await context.EmployeeSalaries.ExecuteDeleteAsync();
        await context.EvaluatorSettings.ExecuteDeleteAsync();

        await context.Employees.ExecuteUpdateAsync(e => e.SetProperty(x => x.EvaluatorEmployeeId, (long?)null));
        await context.Employees.ExecuteUpdateAsync(e => e.SetProperty(x => x.UserId, (long?)null));

        await context.Employees.ExecuteDeleteAsync();
        await context.VariableCompensationParameters.ExecuteDeleteAsync();
        await context.OrganizationUnits.ExecuteDeleteAsync();

        var demoEmails = DemoUserEmails();
        var usersToRemove = await context.Users
            .Where(u => !demoEmails.Contains(u.Email))
            .Select(u => u.Id)
            .ToListAsync();

        if (usersToRemove.Count > 0)
        {
            await context.RefreshTokens.Where(t => usersToRemove.Contains(t.UserId)).ExecuteDeleteAsync();
            await context.UserRoles.Where(r => usersToRemove.Contains(r.UserId)).ExecuteDeleteAsync();
            await context.Users.Where(u => usersToRemove.Contains(u.Id)).ExecuteDeleteAsync();
        }
    }

    private static async Task SeedOrganizationUnitsAsync(AppDbContext context)
    {
        foreach (var (code, name, _, _) in OrgUnitDefinitions)
        {
            context.OrganizationUnits.Add(new OrganizationUnit
            {
                Name = name,
                Code = code,
                IsActive = true,
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedEmployeesAsync(AppDbContext context)
    {
        var orgUnits = await context.OrganizationUnits.OrderBy(o => o.Code).ToListAsync();
        var positions = await context.JobPositions.ToDictionaryAsync(p => p.Name, p => p.Id);
        var education = await context.EducationLevels.OrderBy(e => e.SortOrder).FirstAsync();

        var evaluatorPositionId = positions["Rukovodilac"];
        var controllerPositionId = positions["Menadžer"];
        var staffPositionNames = new[] { "Pripravnik", "Saradnik", "Viši saradnik", "Referent" };
        var staffPositionIds = staffPositionNames.Select(n => positions[n]).ToArray();

        var namePool = BuildUniqueNamePool(ExpectedActiveEmployees);
        var nameIndex = 0;
        var evaluators = new List<Employee>();
        var controllers = new List<Employee>();

        for (var unitIndex = 0; unitIndex < orgUnits.Count; unitIndex++)
        {
            var orgUnit = orgUnits[unitIndex];

            var evaluator = CreateEmployee(
                namePool[nameIndex++],
                orgUnit.Id,
                evaluatorPositionId,
                education.Id,
                hiredAt: new DateOnly(2018, 3, 1));
            context.Employees.Add(evaluator);
            await context.SaveChangesAsync();
            evaluators.Add(evaluator);

            if (unitIndex < 2)
            {
                var controller = CreateEmployee(
                    namePool[nameIndex++],
                    orgUnit.Id,
                    controllerPositionId,
                    education.Id,
                    hiredAt: new DateOnly(2017, 6, 1));
                context.Employees.Add(controller);
                await context.SaveChangesAsync();
                controllers.Add(controller);
            }

            var staffCount = unitIndex < 2 ? 18 : 19;
            for (var i = 0; i < staffCount; i++)
            {
                var positionId = staffPositionIds[(nameIndex + i) % staffPositionIds.Length];
                var staff = CreateEmployee(
                    namePool[nameIndex++],
                    orgUnit.Id,
                    positionId,
                    education.Id,
                    hiredAt: new DateOnly(2019 + (nameIndex % 5), 1 + (nameIndex % 11), 1));
                staff.EvaluatorEmployeeId = evaluator.Id;
                context.Employees.Add(staff);
            }

            await context.SaveChangesAsync();
        }

        for (var i = 0; i < evaluators.Count; i++)
        {
            var controller = controllers[i < 3 ? 0 : 1];
            context.EvaluatorSettings.Add(new EvaluatorSettings
            {
                EmployeeId = evaluators[i].Id,
                ControllerEmployeeId = controller.Id,
            });
        }

        await context.SaveChangesAsync();
    }

    private static Employee CreateEmployee(
        (string First, string Last) name,
        long orgUnitId,
        long jobPositionId,
        long educationLevelId,
        DateOnly hiredAt) =>
        new()
        {
            FirstName = name.First,
            LastName = name.Last,
            OrganizationUnitId = orgUnitId,
            JobPositionId = jobPositionId,
            EducationLevelId = educationLevelId,
            IsActive = true,
            HiredAt = hiredAt,
        };

    private static List<(string First, string Last)> BuildUniqueNamePool(int count)
    {
        var names = new List<(string First, string Last)>(count);

        for (var i = 0; i < count; i++)
        {
            var useFemale = i % 3 == 1;
            var firstList = useFemale ? FemaleFirstNames : MaleFirstNames;
            var first = firstList[i % firstList.Length];
            var last = LastNames[(i / firstList.Length) % LastNames.Length];
            names.Add((first, last));
        }

        return names;
    }

    private static async Task SeedUsersAsync(AppDbContext context)
    {
        await EnsureUserAsync(context, "admin@local.dev", "Admin123!", RoleCodes.Admin);
        await EnsureUserAsync(context, "payroll@local.dev", "Payroll123!", RoleCodes.Payroll);

        var settings = await context.EvaluatorSettings
            .Include(s => s.Employee)
            .OrderBy(s => s.Employee.OrganizationUnitId)
            .ThenBy(s => s.EmployeeId)
            .ToListAsync();

        // Everyone acting as an evaluator or a controller gets an account with
        // the matching role. Without it they cannot sign in, and the role is
        // what makes them an evaluator or a controller in the first place.
        for (var i = 0; i < settings.Count; i++)
        {
            await EnsureUserAsync(
                context,
                EvaluatorEmail(i),
                "Eval123!",
                RoleCodes.Evaluator,
                settings[i].EmployeeId);
        }

        var controllerEmployeeIds = settings
            .Select(s => s.ControllerEmployeeId)
            .Distinct()
            .ToList();

        for (var i = 0; i < controllerEmployeeIds.Count; i++)
        {
            await EnsureUserAsync(
                context,
                ControllerEmail(i),
                "Control123!",
                RoleCodes.Controller,
                controllerEmployeeIds[i]);
        }
    }

    private static async Task EnsureUserAsync(
        AppDbContext context,
        string email,
        string password,
        string roleCode,
        long? employeeId = null)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            var role = await context.Roles.FirstAsync(r => r.Code == roleCode);
            user = new Domain.Entities.Identity.User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
                IsActive = true,
                EmailVerifiedAt = DateTime.UtcNow,
                UserRoles =
                {
                    new Domain.Entities.Identity.UserRole
                    {
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow,
                    },
                },
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
        else
        {
            var role = await context.Roles.FirstAsync(r => r.Code == roleCode);
            var hasRole = await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
            if (!hasRole)
            {
                context.UserRoles.Add(new Domain.Entities.Identity.UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow,
                });
                await context.SaveChangesAsync();
            }
        }

        if (employeeId is null)
        {
            return;
        }

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId.Value);
        if (employee is not null && employee.UserId != user.Id)
        {
            employee.UserId = user.Id;
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedSalariesAsync(AppDbContext context, ISensitiveDataEncryptionService encryption)
    {
        var employees = await context.Employees
            .Include(e => e.JobPosition)
            .Where(e => e.IsActive)
            .ToListAsync();

        foreach (var employee in employees)
        {
            var points = employee.JobPosition.SortOrder * 10;
            foreach (var period in BuildSalaryPeriods(points))
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

    private static IEnumerable<(int Points, decimal SalaryPerPoint, DateOnly From, DateOnly? To)> BuildSalaryPeriods(int points) =>
    [
        (points, SalaryPerPoint, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
        (points, SalaryPerPoint, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
        (points, SalaryPerPoint, new DateOnly(2026, 1, 1), null),
    ];

    private static async Task SeedCompensationParametersAsync(AppDbContext context)
    {
        var orgUnits = await context.OrganizationUnits.ToDictionaryAsync(o => o.Code!, o => o.Id);

        foreach (var (code, _, pool2024, pool2025) in OrgUnitDefinitions)
        {
            if (!orgUnits.TryGetValue(code, out var orgId))
            {
                continue;
            }

            context.VariableCompensationParameters.AddRange(
                CreateParameters(orgId, 2024, pool2024),
                CreateParameters(orgId, 2025, pool2025));
        }

        await context.SaveChangesAsync();
    }

    private static VariableCompensationParameters CreateParameters(long orgUnitId, short year, decimal pool) =>
        new()
        {
            OrganizationUnitId = orgUnitId,
            Year = year,
            MonetaryPool = pool,
            Currency = "RSD",
            AcceptablePerformanceRating = 2.5m,
            DependencyWeight = 1.0m,
            Exponent = 1.5m,
            AllowNegativeVariable = false,
            IsActive = true,
        };

    private static async Task SeedEvaluationsAsync(AppDbContext context)
    {
        var evaluatorSettings = await context.EvaluatorSettings
            .Include(s => s.Employee)
            .Include(s => s.Controller)
            .ToListAsync();

        var evaluatorById = evaluatorSettings.ToDictionary(s => s.EmployeeId);
        var ratingLevels = await context.RatingLevels.AsNoTracking().ToListAsync();
        var measureTypes = await context.MeasureTypes.AsNoTracking().OrderBy(m => m.SortOrder).ToListAsync();
        var descriptiveRatings = await context.DescriptiveRatings.AsNoTracking().ToListAsync();
        var scoring = new EvaluationScoringService();

        var evaluatees = await context.Employees
            .Where(e => e.IsActive && e.EvaluatorEmployeeId != null)
            .ToListAsync();

        var evaluations = new List<Domain.Entities.Evaluation.Evaluation>();

        foreach (var employee in evaluatees)
        {
            if (!evaluatorById.TryGetValue(employee.EvaluatorEmployeeId!.Value, out var settings))
            {
                continue;
            }

            var evaluatorId = settings.EmployeeId;
            var controllerId = settings.ControllerEmployeeId;

            foreach (var (year, quarter, status, includeMeasures) in BuildEvaluationSchedule())
            {
                int[] goalRatings;
                int[] measureRatings;

                if (status == EvaluationStatus.Draft && !includeMeasures)
                {
                    goalRatings = [0, 0, 0];
                    measureRatings = [];
                }
                else
                {
                    goalRatings = GenerateGoalRatings(employee.Id, year, quarter);
                    measureRatings = GenerateMeasureRatings(goalRatings, employee.Id, year, quarter, measureTypes.Count);
                }

                var plan = new DemoEvaluationSeeder.QuarterPlan(
                    year,
                    quarter,
                    status,
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

                evaluations.Add(evaluation);
            }
        }

        context.Evaluations.AddRange(evaluations);
        await context.SaveChangesAsync();
    }

    private static IEnumerable<(short Year, byte Quarter, EvaluationStatus Status, bool IncludeMeasures)> BuildEvaluationSchedule()
    {
        for (byte quarter = 1; quarter <= 4; quarter++)
        {
            yield return (2024, quarter, EvaluationStatus.Approved, true);
        }

        for (byte quarter = 1; quarter <= 4; quarter++)
        {
            yield return (2025, quarter, EvaluationStatus.Approved, true);
        }

        yield return (2026, 1, EvaluationStatus.Approved, true);
        yield return (2026, 2, EvaluationStatus.Approved, true);
        yield return (2026, 3, EvaluationStatus.Draft, false);
    }

    private static int[] GenerateGoalRatings(long employeeId, short year, byte quarter)
    {
        var rng = CreateRng(employeeId, year, quarter, 1);
        return
        [
            PickWeightedRating(rng),
            PickWeightedRating(rng),
            PickWeightedRating(rng),
        ];
    }

    private static int[] GenerateMeasureRatings(
        int[] goalRatings,
        long employeeId,
        short year,
        byte quarter,
        int measureCount)
    {
        var baseRating = (int)Math.Round(goalRatings.Average());
        var rng = CreateRng(employeeId, year, quarter, 2);
        var ratings = new int[measureCount];

        for (var i = 0; i < measureCount; i++)
        {
            var delta = rng.Next(-1, 2);
            ratings[i] = Math.Clamp(baseRating + delta, 1, 5);
        }

        return ratings;
    }

    private static Random CreateRng(long employeeId, short year, byte quarter, int salt) =>
        new(HashCode.Combine(employeeId, year, quarter, salt, SeedVersion));

    private static int PickWeightedRating(Random rng)
    {
        var roll = rng.Next(100);
        var cumulative = 0;
        for (var i = 0; i < RatingWeights.Length; i++)
        {
            cumulative += RatingWeights[i];
            if (roll < cumulative)
            {
                return i + 1;
            }
        }

        return 3;
    }
}
