using Ghuri.Domain.Entities.Catalog;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Finds every class in this project implementing
        // IEntityTypeConfiguration<T> (like CountryConfiguration) and applies
        // it automatically. This means adding a new entity later never
        // requires touching this file - just add its Configuration class.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
