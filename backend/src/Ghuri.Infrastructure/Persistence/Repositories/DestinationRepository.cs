using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IDestinationRepository. The soft-delete filter hides deleted rows from every query here.</summary>
internal sealed class DestinationRepository(AppDbContext db) : IDestinationRepository
{
    /// <summary>Loads the photo gallery too - it's part of the destination (one aggregate).</summary>
    public Task<Destination?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Destinations.Include(d => d.Images).FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Destinations.AnyAsync(d => d.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken) =>
        db.Destinations.AnyAsync(d => d.Slug == slug && d.Id != exceptId, cancellationToken);

    public Task<bool> CountryExistsAsync(short countryId, CancellationToken cancellationToken) =>
        db.Countries.AnyAsync(c => c.Id == countryId, cancellationToken);

    public Task<bool> IsUsedByPackagesAsync(Guid destinationId, CancellationToken cancellationToken) =>
        db.TourPackages.AnyAsync(p => p.DestinationId == destinationId, cancellationToken);

    public void Add(Destination destination) => db.Destinations.Add(destination);
}
