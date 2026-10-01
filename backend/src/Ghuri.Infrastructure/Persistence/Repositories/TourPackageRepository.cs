using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of ITourPackageRepository. The soft-delete filter hides deleted rows from every query here.</summary>
internal sealed class TourPackageRepository(AppDbContext db) : ITourPackageRepository
{
    // A sequence name can't be a SQL parameter, so it's part of the text -
    // safe, because it's our own constant, never user input. "AS [Value]"
    // is the column name EF Core expects when it reads a single number.
    private const string NextCodeSql =
        "SELECT NEXT VALUE FOR [catalog].[" + AppDbContext.PackageCodeSequenceName + "] AS [Value]";

    /// <summary>
    /// One SELECT per list instead of one big JOIN - split queries are the
    /// default (see AddPersistence). Joined, photos × days × categories
    /// would return every combination: 450 rows for one package instead of 29.
    /// </summary>
    public Task<TourPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.TourPackages
            .Include(p => p.Images)
            .Include(p => p.ItineraryDays)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken) =>
        db.TourPackages.AnyAsync(p => p.Slug == slug && p.Id != exceptId, cancellationToken);

    public async Task<string> NextPackageCodeAsync(CancellationToken cancellationToken)
    {
        // ToListAsync, NOT SingleAsync/FirstAsync: those make EF wrap the SQL
        // as "SELECT TOP(2) ... FROM (our SQL) AS s", and SQL Server forbids
        // NEXT VALUE FOR inside a subquery. ToListAsync sends it unchanged.
        var values = await db.Database.SqlQueryRaw<int>(NextCodeSql).ToListAsync(cancellationToken);
        return $"PKG{values.Single()}";
    }

    public void Add(TourPackage package) => db.TourPackages.Add(package);
}
