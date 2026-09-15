using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Domain.Entities.Evaluation;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Enums;
using VariableCompensation.Infrastructure.Persistence;
using VariableCompensation.Testing.Common.Fixtures;

namespace VariableCompensation.Testing.Common.Seeding;

public static class TestDataSeeder
{
    public static async Task SeedAsync(AppDbContext context, ISensitiveDataEncryptionService encryption)
    {
        await context.Database.MigrateAsync();
        await SeedLookupsAsync(context);
        await SeedUsersAndEmployeesAsync(context);
        await SeedApprovedEvaluationAsync(context);
    }

    private static async Task SeedLookupsAsync(AppDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { Code = RoleCodes.Admin, Name = "Administrator" },
                new Role { Code = RoleCodes.Evaluator, Name = "Ocenjivač" },
                new Role { Code = RoleCodes.Controller, Name = "Kontroler" },
                new Role { Code = RoleCodes.Employee, Name = "Zaposleni" },
                new Role { Code = RoleCodes.Payroll, Name = "Plate i varijabila" });
            await context.SaveChangesAsync();
        }

        if (!await context.RatingLevels.AnyAsync())
        {
            foreach (var level in RatingLevelsFixture.Create())
            {
                context.RatingLevels.Add(level);
            }

            await context.SaveChangesAsync();
        }

        if (!await context.DescriptiveRatings.AnyAsync())
        {
            foreach (var rating in DescriptiveRatingsFixture.Create())
            {
                context.DescriptiveRatings.Add(rating);
            }

            await context.SaveChangesAsync();
        }

        if (!await context.OrganizationUnits.AnyAsync())
        {
            context.OrganizationUnits.Add(new OrganizationUnit { Code = "IT", Name = "IT", IsActive = true });
            await context.SaveChangesAsync();
        }

        if (!await context.JobPositions.AnyAsync())
        {
            context.JobPositions.Add(new JobPosition { Name = "Saradnik", SortOrder = 2, IsActive = true });
            await context.SaveChangesAsync();
        }

        if (!await context.EducationLevels.AnyAsync())
        {
            context.EducationLevels.Add(new EducationLevel { Name = "Master", SortOrder = 5, IsActive = true });
            await context.SaveChangesAsync();
        }

        if (!await context.MeasureTypes.AnyAsync())
        {
            context.MeasureTypes.Add(new MeasureType { Code = "INITIATIVE", Name = "Inicijativa", IsActive = true });
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedUsersAndEmployeesAsync(AppDbContext context)
    {
        var orgUnit = await context.OrganizationUnits.FirstAsync();
        var jobPosition = await context.JobPositions.FirstAsync();
        var education = await context.EducationLevels.FirstAsync();
        var roles = await context.Roles.ToDictionaryAsync(r => r.Code, r => r);

        async Task EnsureUserAsync(string email, string password, string roleCode, Employee? employee = null)
        {
            if (await context.Users.AnyAsync(u => u.Email == email))
            {
                return;
            }

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
                IsActive = true,
                EmailVerifiedAt = DateTime.UtcNow,
            };

            user.UserRoles.Add(new UserRole
            {
                Role = roles[roleCode],
                AssignedAt = DateTime.UtcNow,
            });

            if (employee is not null)
            {
                employee.User = user;
                context.Employees.Add(employee);
            }
            else
            {
                context.Users.Add(user);
            }
        }

        var evaluator = new Employee
        {
            FirstName = "Jovan",
            LastName = "Ocenjivač",
            OrganizationUnitId = orgUnit.Id,
            JobPositionId = jobPosition.Id,
            EducationLevelId = education.Id,
            IsActive = true,
        };

        var controller = new Employee
        {
            FirstName = "Milan",
            LastName = "Kontroler",
            OrganizationUnitId = orgUnit.Id,
            JobPositionId = jobPosition.Id,
            EducationLevelId = education.Id,
            IsActive = true,
        };

        var employee = new Employee
        {
            FirstName = "Marko",
            LastName = "Marković",
            OrganizationUnitId = orgUnit.Id,
            JobPositionId = jobPosition.Id,
            EducationLevelId = education.Id,
            IsActive = true,
        };

        if (!await context.Employees.AnyAsync())
        {
            await EnsureUserAsync(TestCredentials.EvaluatorEmail, TestCredentials.EvaluatorPassword, RoleCodes.Evaluator, evaluator);
            await EnsureUserAsync(TestCredentials.ControllerEmail, TestCredentials.ControllerPassword, RoleCodes.Controller, controller);
            await context.SaveChangesAsync();

            employee.EvaluatorEmployeeId = evaluator.Id;
            await EnsureUserAsync(TestCredentials.EmployeeEmail, TestCredentials.EmployeePassword, RoleCodes.Employee, employee);
            await EnsureUserAsync(TestCredentials.AdminEmail, TestCredentials.AdminPassword, RoleCodes.Admin);
            await EnsureUserAsync(TestCredentials.PayrollEmail, TestCredentials.PayrollPassword, RoleCodes.Payroll);
            await context.SaveChangesAsync();

            TestEmployeeIds.Evaluator = evaluator.Id;
            TestEmployeeIds.Controller = controller.Id;
            TestEmployeeIds.Employee = employee.Id;

            context.EvaluatorSettings.Add(new EvaluatorSettings
            {
                EmployeeId = evaluator.Id,
                ControllerEmployeeId = controller.Id,
                ThresholdDoesNotMeet = 2.0m,
                ThresholdMeets = 2.5m,
                ThresholdGood = 3.5m,
                ThresholdExceeds = 4.5m,
                PercentDoesNotMeet = 0.05m,
                PercentMeets = 0.10m,
                PercentGood = 0.15m,
                PercentExceeds = 0.20m,
            });
            await context.SaveChangesAsync();
        }
        else
        {
            TestEmployeeIds.Evaluator = await context.Employees.Where(e => e.FirstName == "Jovan").Select(e => e.Id).FirstAsync();
            TestEmployeeIds.Controller = await context.Employees.Where(e => e.FirstName == "Milan").Select(e => e.Id).FirstAsync();
            TestEmployeeIds.Employee = await context.Employees.Where(e => e.FirstName == "Marko").Select(e => e.Id).FirstAsync();
        }
    }

    private static async Task SeedApprovedEvaluationAsync(AppDbContext context)
    {
        if (await context.Evaluations.AnyAsync())
        {
            return;
        }

        var ratedLevel3 = await context.RatingLevels.FirstAsync(r => r.Value == 3);
        var descriptive = await context.DescriptiveRatings.FirstAsync(d => d.Code == "GOOD");

        var evaluation = new Evaluation
        {
            EmployeeId = TestEmployeeIds.Employee,
            EvaluatorEmployeeId = TestEmployeeIds.Evaluator,
            ControllerEmployeeId = TestEmployeeIds.Controller,
            Year = 2026,
            Quarter = 1,
            Status = EvaluationStatus.Approved,
            GoalsAverage = 3.0m,
            MeasuresAverage = 3.0m,
            OverallAverage = 3.0m,
            DescriptiveRatingId = descriptive.Id,
            ApprovedAt = DateTime.UtcNow,
            Version = 2,
        };

        evaluation.Goals.Add(new EvaluationGoal
        {
            Description = "Test goal",
            RatingLevelId = ratedLevel3.Id,
            Weight = 1,
            SortOrder = 1,
        });

        evaluation.Measures.Add(new EvaluationMeasure
        {
            MeasureTypeId = (await context.MeasureTypes.FirstAsync()).Id,
            RatingLevelId = ratedLevel3.Id,
            SortOrder = 1,
        });

        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();
    }
}
