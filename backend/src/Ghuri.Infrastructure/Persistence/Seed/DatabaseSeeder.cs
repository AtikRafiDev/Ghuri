using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ghuri.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates the data a migration can't hold because it differs per
/// environment: the first Super Admin (blueprint section 15: "one Super
/// Admin user without a password (activated by one-time code)"). Fixed
/// data like the role list lives in migrations instead (RoleConfiguration).
/// </summary>
/// <remarks>
/// The account gets NO password. Whoever owns the configured email sets
/// one through "Forgot password" - which proves they own that inbox - so
/// no password ever sits in a config file, a secret store, or git.
/// Safe to run any number of times: once a Super Admin exists, it does
/// nothing.
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

        await SeedSuperAdminAsync(cancellationToken);
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
        var normalizedEmail = email.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail || u.PhoneNumber == phone, cancellationToken))
            throw new InvalidOperationException(
                $"A user with email '{email}' or phone '{phone}' already exists. " +
                $"Put a different email/phone in {SuperAdminSection}, then seed again.");

        var user = User.Create(fullName, phone, email, passwordHash: null);
        db.Users.Add(user);
        db.UserRoles.Add(UserRole.Create(user.Id, superAdminRoleId, clock.GetUtcNow().UtcDateTime));
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
