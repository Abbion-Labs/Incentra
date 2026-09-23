using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Domain.Entities.Lookup;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class DatabaseSeeder(
    AppDbContext context,
    ISensitiveDataEncryptionService encryption) : IDatabaseSeeder
{
    public async Task SeedAsync()
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
        else if (!await context.Roles.AnyAsync(r => r.Code == RoleCodes.Payroll))
        {
            context.Roles.Add(new Role { Code = RoleCodes.Payroll, Name = "Plate i varijabila" });
            await context.SaveChangesAsync();
        }

        if (!await context.DescriptiveRatings.AnyAsync())
        {
            context.DescriptiveRatings.AddRange(
                new DescriptiveRating { Code = "DOES_NOT_MEET", Name = "Ne zadovoljava", MinAverage = 0.00m, MaxAverage = 1.99m, SortOrder = 1, RecommendedShare = 0.05m },
                new DescriptiveRating { Code = "MEETS", Name = "Zadovoljava", MinAverage = 2.00m, MaxAverage = 2.99m, SortOrder = 2, RecommendedShare = 0.15m },
                new DescriptiveRating { Code = "GOOD", Name = "Dobar", MinAverage = 3.00m, MaxAverage = 3.49m, SortOrder = 3, RecommendedShare = 0.45m },
                new DescriptiveRating { Code = "EXCEEDS", Name = "Ističe se", MinAverage = 3.50m, MaxAverage = 4.49m, SortOrder = 4, RecommendedShare = 0.30m },
                new DescriptiveRating { Code = "OUTSTANDING", Name = "Naročito se ističe", MinAverage = 4.50m, MaxAverage = 5.00m, SortOrder = 5, RecommendedShare = 0.05m });
            await context.SaveChangesAsync();
        }

        if (!await context.RatingLevels.AnyAsync())
        {
            context.RatingLevels.AddRange(
                new RatingLevel { Value = 0, Label = "/", Description = "Nije ocenjeno." },
                new RatingLevel { Value = 1, Label = "Ne zadovoljava", Description = "Zaposleni nije ostvario utvrđene radne ciljeve." },
                new RatingLevel { Value = 2, Label = "Minimalno zadovoljava", Description = "Zaposleni je sa minimalnim rezultatom ostvario ciljeve." },
                new RatingLevel { Value = 3, Label = "Zadovoljava", Description = "Zaposleni je sa prosečnim rezultatom ostvario ciljeve." },
                new RatingLevel { Value = 4, Label = "Ističe se", Description = "Zaposleni značajno prevazilazi očekivanja." },
                new RatingLevel { Value = 5, Label = "Naročito se ističe", Description = "Zaposleni po svim kriterijumima značajno prevazilazi očekivanja." });
            await context.SaveChangesAsync();
        }
        else if (!await context.RatingLevels.AnyAsync(r => r.Value == 0))
        {
            context.RatingLevels.Add(new RatingLevel { Value = 0, Label = "/", Description = "Nije ocenjeno." });
            await context.SaveChangesAsync();
        }

        if (!await context.MeasureTypes.AnyAsync())
        {
            context.MeasureTypes.AddRange(CreateDefaultMeasureTypes());
            await context.SaveChangesAsync();
        }

        await EnsureMeasureTypeDefinitionsAsync();

        if (!await context.Users.AnyAsync())
        {
            var adminRole = await context.Roles.FirstAsync(r => r.Code == RoleCodes.Admin);
            context.Users.Add(new Domain.Entities.Identity.User
            {
                Email = "admin@local.dev",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", workFactor: 12),
                IsActive = true,
                EmailVerifiedAt = DateTime.UtcNow,
                UserRoles =
                {
                    new Domain.Entities.Identity.UserRole
                    {
                        RoleId = adminRole.Id,
                        AssignedAt = DateTime.UtcNow
                    }
                }
            });
            await context.SaveChangesAsync();
        }

        if (!await context.JobPositions.AnyAsync())
        {
            context.JobPositions.AddRange(
                new JobPosition { Name = "Pripravnik", SortOrder = 1, IsActive = true },
                new JobPosition { Name = "Saradnik", SortOrder = 2, IsActive = true },
                new JobPosition { Name = "Viši saradnik", SortOrder = 3, IsActive = true },
                new JobPosition { Name = "Referent", SortOrder = 4, IsActive = true },
                new JobPosition { Name = "Menadžer", SortOrder = 5, IsActive = true },
                new JobPosition { Name = "Rukovodilac", SortOrder = 6, IsActive = true },
                new JobPosition { Name = "Direktor", SortOrder = 7, IsActive = true });
            await context.SaveChangesAsync();
        }

        if (!await context.EducationLevels.AnyAsync())
        {
            context.EducationLevels.AddRange(
                new EducationLevel { Name = "Srednja stručna sprema", SortOrder = 1, IsActive = true },
                new EducationLevel { Name = "Strukovni inženjer", SortOrder = 2, IsActive = true },
                new EducationLevel { Name = "Diplomirani inženjer", SortOrder = 3, IsActive = true },
                new EducationLevel { Name = "Diplomirani ekonomista", SortOrder = 4, IsActive = true },
                new EducationLevel { Name = "Master", SortOrder = 5, IsActive = true });
            await context.SaveChangesAsync();
        }

        await Persistence.RealisticOrganizationSeeder.EnsureAsync(context, encryption);
    }

    private static IReadOnlyList<MeasureType> CreateDefaultMeasureTypes() =>
        MeasureTypeDefinitions
            .Select((definition, index) => new MeasureType
            {
                Code = definition.Code,
                Name = definition.Name,
                Description = definition.Description,
                SortOrder = index + 1,
            })
            .ToList();

    private async Task EnsureMeasureTypeDefinitionsAsync()
    {
        var changed = false;
        foreach (var definition in MeasureTypeDefinitions)
        {
            var measureType = await context.MeasureTypes.FirstOrDefaultAsync(m => m.Code == definition.Code);
            if (measureType is null)
            {
                context.MeasureTypes.Add(new MeasureType
                {
                    Code = definition.Code,
                    Name = definition.Name,
                    Description = definition.Description,
                    SortOrder = definition.SortOrder,
                });
                changed = true;
                continue;
            }

            if (measureType.Name != definition.Name ||
                measureType.Description != definition.Description ||
                measureType.SortOrder != definition.SortOrder)
            {
                measureType.Name = definition.Name;
                measureType.Description = definition.Description;
                measureType.SortOrder = definition.SortOrder;
                changed = true;
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }
    }

    private static readonly MeasureTypeDefinition[] MeasureTypeDefinitions =
    [
        new(
            "INITIATIVE",
            "Preduzimljivost",
            1,
            "Planiranje i realizacija radnih ciljeva bez posebnih usmeravanja i upućivanja u posao, pokazivanje inicijative u obavljanju poslova."),
        new(
            "CREATIVITY",
            "Stvaralačka sposobnost",
            2,
            "Pronalaženje novih ili najboljih rešenja, u skladu sa propisima i pravilima struke, kao i davanje predloga za unapređenje postupaka ili poboljšanje načina rada na svom radnom mestu ili u organizacionoj celini."),
        new(
            "INDEPENDENCE",
            "Samostalnost",
            3,
            "Sposobnost da bez oslanjanja na pomoć i savete okoline (kolega i rukovodilaca) obavlja poslove i zadatke ili da rešava nastale probleme u poslu."),
        new(
            "PRECISION",
            "Preciznost i savesnost",
            4,
            "Tačnost i preciznost u obavljanju zadataka, sa vođenjem računa o detaljima; poštovanje rokova, radnog vremena i sl."),
        new(
            "COLLABORATION",
            "Kvalitet saradnje",
            5,
            "Svrsishodna komunikacija sa rukovodiocem, spremnost za razmenu ideja i saveta sa kolegama, izbegavanje konflikata, podsticanje dobre saradnje, uvažavanje saradnika."),
        new(
            "ADDITIONAL",
            "Dodatna merila",
            6,
            "Specijalne veštine neophodne za obavljanje poslova i zadataka radnog mesta; na primer: poznavanje relevantnih zakona i drugih propisa."),
    ];

    private sealed record MeasureTypeDefinition(string Code, string Name, int SortOrder, string Description);
}
