using Kvitoria.Models;
using Kvitoria.Models.Auth;
using Kvitoria.Models.Enums;
using Kvitoria.Models.PlantCatalog;
using Kvitoria.ViewModels.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Kvitoria.Data;

public static class KvitoriaDbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var shouldMigrate = configuration.GetValue("Database:AutoMigrate", true);
        var shouldSeed = configuration.GetValue("Database:SeedDemoData", true);

        if (!shouldMigrate && !shouldSeed)
        {
            return;
        }

        try
        {
            var dbContext = services.GetRequiredService<KvitoriaDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            if (shouldMigrate)
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            var demoUser = await EnsureIdentitySeedAsync(userManager, roleManager, configuration);

            await EnsurePlantCatalogSeedAsync(dbContext, cancellationToken);

            if (shouldSeed && !await dbContext.Plants.AnyAsync(cancellationToken))
            {
                await SeedDemoCollectionAsync(dbContext, demoUser.Id, cancellationToken);
            }
            else if (await dbContext.Plants.AnyAsync(plant => plant.UserId == null, cancellationToken))
            {
                var orphanPlants = await dbContext.Plants
                    .Where(plant => plant.UserId == null)
                    .ToListAsync(cancellationToken);

                foreach (var plant in orphanPlants)
                {
                    plant.AssignOwner(demoUser.Id);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            logger.LogWarning(exception, "PostgreSQL is not available yet. Kvitoria will start and show database setup guidance.");
        }
    }

    private static async Task<ApplicationUser> EnsureIdentitySeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        foreach (var role in new[] { ApplicationRoleNames.Admin, ApplicationRoleNames.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = configuration["Seed:Admin:Email"] ?? "admin@kvitoria.local";
        var adminPassword = configuration["Seed:Admin:Password"] ?? "Admin123!";
        var adminLogin = configuration["Seed:Admin:Login"] ?? "admin";
        var admin = await EnsureUserAsync(userManager, adminEmail, adminLogin, "Адміністратор Kvitoria", new DateOnly(1990, 1, 1), adminPassword);

        if (!await userManager.IsInRoleAsync(admin, ApplicationRoleNames.Admin))
        {
            await userManager.AddToRoleAsync(admin, ApplicationRoleNames.Admin);
        }

        var demoEmail = configuration["Seed:DemoUser:Email"] ?? "user@kvitoria.local";
        var demoPassword = configuration["Seed:DemoUser:Password"] ?? "User123!";
        var demoLogin = configuration["Seed:DemoUser:Login"] ?? "user.demo";
        var demoUser = await EnsureUserAsync(userManager, demoEmail, demoLogin, "Колекціонер Kvitoria", new DateOnly(1995, 5, 20), demoPassword);

        if (!await userManager.IsInRoleAsync(demoUser, ApplicationRoleNames.User))
        {
            await userManager.AddToRoleAsync(demoUser, ApplicationRoleNames.User);
        }

        await NormalizeExistingLoginsAsync(userManager);

        return demoUser;
    }

    private static async Task NormalizeExistingLoginsAsync(UserManager<ApplicationUser> userManager)
    {
        var users = await userManager.Users.ToListAsync();
        var usedLogins = users
            .Select(user => user.UserName)
            .Where(IsValidLogin)
            .Select(login => login!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var user in users.Where(user => !IsValidLogin(user.UserName)))
        {
            var candidate = BuildLoginFromEmail(user.Email, user.Id);
            var baseCandidate = candidate;
            var suffix = 2;

            while (usedLogins.Contains(candidate))
            {
                candidate = $"{baseCandidate}.{suffix}";
                suffix++;
            }

            await userManager.SetUserNameAsync(user, candidate);
            await userManager.UpdateAsync(user);
            usedLogins.Add(candidate);
        }
    }

    private static bool IsValidLogin(string? login)
    {
        return !string.IsNullOrWhiteSpace(login)
            && Regex.IsMatch(login, AccountValidationRules.LoginPattern);
    }

    private static string BuildLoginFromEmail(string? email, string userId)
    {
        var source = (email?.Split('@')[0] ?? $"user{userId}")
            .Trim()
            .ToLowerInvariant();
        var characters = source
            .Select(character =>
                character is >= 'a' and <= 'z'
                    || character is >= '0' and <= '9'
                    || character is '.' or '_'
                    ? character
                    : '.')
            .ToArray();
        var candidate = new string(characters).Trim('.');

        if (string.IsNullOrWhiteSpace(candidate) || candidate[0] is < 'a' or > 'z')
        {
            candidate = $"user.{candidate}";
        }

        while (candidate.Length < 5)
        {
            candidate += "0";
        }

        return candidate;
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string login,
        string fullName,
        DateOnly birthDate,
        string password)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            var normalizedLogin = AccountValidationRules.NormalizeLogin(login);

            if (!string.Equals(user.UserName, normalizedLogin, StringComparison.Ordinal))
            {
                await userManager.SetUserNameAsync(user, normalizedLogin);
            }

            user.UpdateProfile(fullName, birthDate);

            if (user.PasswordHash?.StartsWith("SHA256$", StringComparison.Ordinal) != true)
            {
                user.PasswordHash = userManager.PasswordHasher.HashPassword(user, password);
            }

            await userManager.UpdateAsync(user);
            return user;
        }

        login = AccountValidationRules.NormalizeLogin(login);
        user = new ApplicationUser
        {
            UserName = login,
            Email = email,
            EmailConfirmed = true
        };
        user.UpdateProfile(fullName, birthDate);

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Cannot seed user {email}: {errors}");
        }

        return user;
    }

    private static async Task EnsurePlantCatalogSeedAsync(
        KvitoriaDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var catalog = new (string Species, string? Variety, PlantVariantType Type)[]
        {
            ("Monstera deliciosa", "Albo Variegata", PlantVariantType.Variety),
            ("Hoya carnosa", "Krimson Queen", PlantVariantType.Variety),
            ("Calathea orbifolia", null, PlantVariantType.Form),
            ("Philodendron hederaceum", "Brasil", PlantVariantType.Variety),
            ("Epipremnum aureum", "Marble Queen", PlantVariantType.Variety)
        };

        foreach (var item in catalog)
        {
            var species = await dbContext.PlantSpecies
                .Include(entity => entity.Varieties)
                .FirstOrDefaultAsync(entity => entity.Name == item.Species, cancellationToken);

            if (species is null)
            {
                species = new PlantSpecies(item.Species);
                dbContext.PlantSpecies.Add(species);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(item.Variety)
                && species.Varieties.All(variety =>
                    !string.Equals(variety.Name, item.Variety, StringComparison.OrdinalIgnoreCase)
                    || variety.Type != item.Type))
            {
                dbContext.PlantVarieties.Add(new PlantVariety(species.Id, item.Variety, item.Type));
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static async Task SeedDemoCollectionAsync(
        KvitoriaDbContext dbContext,
        string demoUserId,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monsteraSpecies = await dbContext.PlantSpecies.FirstAsync(item => item.Name == "Monstera deliciosa", cancellationToken);
        var monsteraVariety = await dbContext.PlantVarieties.FirstAsync(item => item.PlantSpeciesId == monsteraSpecies.Id && item.Name == "Albo Variegata", cancellationToken);
        var hoyaSpecies = await dbContext.PlantSpecies.FirstAsync(item => item.Name == "Hoya carnosa", cancellationToken);
        var hoyaVariety = await dbContext.PlantVarieties.FirstAsync(item => item.PlantSpeciesId == hoyaSpecies.Id && item.Name == "Krimson Queen", cancellationToken);
        var calatheaSpecies = await dbContext.PlantSpecies.FirstAsync(item => item.Name == "Calathea orbifolia", cancellationToken);

        var monstera = new Plant(
            "Монстера Альба",
            monsteraSpecies.Name,
            PlantStatus.Growing,
            LightRequirement.BrightIndirect,
            WateringFrequency.Weekly);
        monstera.AssignOwner(demoUserId);
        monstera.UpdateProfile(
            monstera.Name,
            monstera.Species,
            "Albo Variegata",
            monstera.Status,
            monstera.LightRequirement,
            monstera.WateringFrequency,
            "Східне вікно",
            18,
            today.AddMonths(-7),
            "Обмін у колекціонерки",
            "https://images.unsplash.com/photo-1614594975525-e45190c55d0b?auto=format&fit=crop&w=900&q=80",
            "Тримати стабільну вологість без переливу.");
        monstera.AssignCatalog(monsteraSpecies, monsteraVariety);
        monstera.UpdateCareSchedule(today.AddDays(-5));

        var hoya = new Plant(
            "Хоя Карноза",
            hoyaSpecies.Name,
            PlantStatus.Adapting,
            LightRequirement.BrightIndirect,
            WateringFrequency.EveryTenDays);
        hoya.AssignOwner(demoUserId);
        hoya.UpdateProfile(
            hoya.Name,
            hoya.Species,
            "Krimson Queen",
            hoya.Status,
            hoya.LightRequirement,
            hoya.WateringFrequency,
            "Полиця з фітолампою",
            12,
            today.AddMonths(-2),
            "Розсадник",
            "https://images.unsplash.com/photo-1610630871665-532db8c8f1ab?auto=format&fit=crop&w=900&q=80",
            "Перевірити нові пагони після адаптації.");
        hoya.AssignCatalog(hoyaSpecies, hoyaVariety);
        hoya.UpdateCareSchedule(today.AddDays(-11));

        var calathea = new Plant(
            "Калатея Орбіфолія",
            calatheaSpecies.Name,
            PlantStatus.NeedsCare,
            LightRequirement.PartialShade,
            WateringFrequency.EveryThreeDays);
        calathea.AssignOwner(demoUserId);
        calathea.UpdateProfile(
            calathea.Name,
            calathea.Species,
            null,
            calathea.Status,
            calathea.LightRequirement,
            calathea.WateringFrequency,
            "Кухня",
            14,
            today.AddMonths(-4),
            "Магазин",
            "https://images.unsplash.com/photo-1598880940080-ff9a29891b85?auto=format&fit=crop&w=900&q=80",
            "Підсихає край листка, підняти вологість.");
        calathea.AssignCatalog(calatheaSpecies, null);
        calathea.UpdateCareSchedule(today.AddDays(-4));

        dbContext.Plants.AddRange(monstera, hoya, calathea);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
