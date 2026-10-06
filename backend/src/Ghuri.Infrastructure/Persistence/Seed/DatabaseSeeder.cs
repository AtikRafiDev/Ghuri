using System.Globalization;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ghuri.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates the data a migration can't hold because it differs per
/// environment or per machine: the countries (from .NET's RegionInfo) and
/// the first Super Admin (blueprint section 15: "one Super Admin user
/// without a password (activated by one-time code)"). Fixed data like the
/// role list lives in migrations instead (RoleConfiguration).
/// </summary>
/// <remarks>
/// The account gets NO password. Whoever owns the configured email sets
/// one through "Forgot password" - which proves they own that inbox - so
/// no password ever sits in a config file, a secret store, or git.
/// Safe to run any number of times: it only adds missing countries, and
/// once a Super Admin exists it doesn't create another.
/// </remarks>
internal sealed class DatabaseSeeder(
    AppDbContext db, IConfiguration configuration, TimeProvider clock, ILogger<DatabaseSeeder> logger)
{
    /// <summary>Settings: Seed:SuperAdmin:FullName / Email / Phone.</summary>
    public const string SuperAdminSection = "Seed:SuperAdmin";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // The roles arrive with a migration. Seeding an out-of-date
        // database would fail on the UserRoles -> Roles foreign key with a
        // far less helpful message than this one.
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
            throw new InvalidOperationException(
                "The database has pending migrations. Run \".\\ef.cmd database update\" first, then seed.");

        await SeedCountriesAsync(cancellationToken);
        await SeedSuperAdminAsync(cancellationToken);
        await SeedCancellationPolicyAsync(cancellationToken);
    }

    /// <summary>
    /// The global refund rule for customer cancellations (decided 2026-10-06):
    /// 30+ days before the trip 100% · 15-29 days 50% · 7-14 days 25% · less 0%.
    /// </summary>
    /// <remarks>
    /// Here and not in a migration: staff will change these rows (Phase 2
    /// admin screen), and a migration would put its own numbers back. So
    /// only when there's no global rule at all - never over staff's changes.
    /// </remarks>
    private async Task SeedCancellationPolicyAsync(CancellationToken cancellationToken)
    {
        if (await db.CancellationPolicies.AnyAsync(p => p.PackageId == null, cancellationToken))
        {
            logger.LogInformation("A global cancellation policy already exists - nothing to seed.");
            return;
        }

        db.CancellationPolicies.AddRange(
            CancellationPolicy.Create(packageId: null, minDaysBefore: 30, refundPercent: 100),
            CancellationPolicy.Create(packageId: null, minDaysBefore: 15, refundPercent: 50),
            CancellationPolicy.Create(packageId: null, minDaysBefore: 7, refundPercent: 25),
            CancellationPolicy.Create(packageId: null, minDaysBefore: 0, refundPercent: 0));
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Global cancellation policy created: 30+ days 100%, 15+ 50%, 7+ 25%, less 0%.");
    }

    /// <summary>
    /// Blueprint: "Countries seeded". The list comes from .NET itself
    /// (RegionInfo - the operating system's ICU country data, ~244 countries
    /// incl. Bangladesh), not from a hand-typed list.
    /// </summary>
    /// <remarks>
    /// Why here and not in a migration (HasData): a migration must give the
    /// SAME rows on every machine forever, but ICU's list and names change
    /// between OS versions ("Turkey" may become "Türkiye"), which would make
    /// EF generate surprise migrations - and ids taken from list positions
    /// would shift, pointing destinations at the wrong country. Here SQL
    /// Server hands out each id once (IDENTITY) and it never changes.
    /// Only ADDS countries that are missing - existing rows (and the ids
    /// destinations point at) are never renamed or removed.
    /// </remarks>
    private async Task SeedCountriesAsync(CancellationToken cancellationToken)
    {
        var available = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            // Skips world/continent "regions" like 001 (World) and 150 (Europe).
            .Where(region => region.TwoLetterISORegionName.Length == 2 && char.IsAsciiLetter(region.TwoLetterISORegionName[0]))
            .DistinctBy(region => region.TwoLetterISORegionName)
            .OrderBy(region => region.EnglishName)
            .ToList();

        // A Linux server in "globalization-invariant" mode (common in small
        // Docker images) has no ICU data at all - fail loudly rather than
        // silently seed zero countries.
        if (available.Count == 0)
            throw new InvalidOperationException(
                "This machine's .NET has no country data (globalization-invariant mode, no ICU). " +
                "Install ICU or set InvariantGlobalization=false, then seed again.");

        var existing = (await db.Countries.Select(c => c.IsoCode).ToListAsync(cancellationToken)).ToHashSet();
        var missing = available.Where(region => !existing.Contains(region.TwoLetterISORegionName)).ToList();

        foreach (var region in missing)
            db.Countries.Add(Country.Create(region.EnglishName, region.TwoLetterISORegionName));
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Countries: added {Added}, already had {Existing}.", missing.Count, existing.Count);
    }

    private async Task SeedSuperAdminAsync(CancellationToken cancellationToken)
    {
        const byte superAdminRoleId = (byte)SystemRole.SuperAdmin;

        if (await db.UserRoles.AnyAsync(ur => ur.RoleId == superAdminRoleId, cancellationToken))
        {
            logger.LogInformation("A Super Admin already exists - nothing to seed.");
            return;
        }

        var settings = configuration.GetSection(SuperAdminSection);
        var fullName = Required(settings, "FullName");
        var email = Required(settings, "Email");
        var phone = PhoneNumber.Create(Required(settings, "Phone"));

        // Never silently promote an existing account (say, a customer who
        // registered with this phone) to Super Admin - make a human decide.
        var normalizedEmail = User.NormalizeEmail(email);
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail || u.PhoneNumber == phone, cancellationToken))
            throw new InvalidOperationException(
                $"A user with email '{email}' or phone '{phone}' already exists. " +
                $"Put a different email/phone in {SuperAdminSection}, then seed again.");

        var user = User.Create(fullName, phone, email, passwordHash: null);
        user.AssignRole(SystemRole.SuperAdmin, clock.GetUtcNow().UtcDateTime);
        db.Users.Add(user); // adding the user also adds its role - they're one aggregate
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Super Admin {Email} created WITHOUT a password - set one with \"Forgot password\" on the login page.",
            email);
    }

    private static string Required(IConfigurationSection section, string key)
    {
        var value = section[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(
                $"Setting '{section.Path}:{key}' is missing - it's needed to create the first Super Admin.")
            : value;
    }
}
