using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Iam;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence;

/// <summary>
/// The write-side database context. Command handlers change data through
/// repositories, which use this context underneath - nothing outside
/// Infrastructure ever references AppDbContext directly.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();

    // iam schema
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    // catalog schema
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TourPackage> TourPackages => Set<TourPackage>();
    public DbSet<PackageCategory> PackageCategories => Set<PackageCategory>();
    public DbSet<PackageImage> PackageImages => Set<PackageImage>();
    public DbSet<ItineraryDay> ItineraryDays => Set<ItineraryDay>();
    public DbSet<PackageAddOn> PackageAddOns => Set<PackageAddOn>();
    public DbSet<Departure> Departures => Set<Departure>();

    /// <summary>
    /// The blueprint's rule (section 5.1): "human-readable numbers (PKG1001
    /// package...) come from SQL SEQUENCE objects" - a real database object
    /// that hands out 1001, 1002, 1003... one at a time, safely, even if
    /// two people create a package at the exact same moment. The repository
    /// (built on Day 4) reads the next value and formats it as "PKG1001".
    /// </summary>
    public const string PackageCodeSequenceName = "PackageCodeSequence";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<int>(PackageCodeSequenceName, schema: "catalog")
            .StartsAt(1001)
            .IncrementsBy(1);

        // Finds every class in this project implementing
        // IEntityTypeConfiguration<T> (like CountryConfiguration) and applies
        // it automatically. This means adding a new entity later never
        // requires touching this file - just add its Configuration class.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
