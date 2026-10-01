using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Repositories;

/// <summary>Destinations (blueprint section 7.1). Deleted destinations are invisible to every method.</summary>
public interface IDestinationRepository
{
    Task<Destination?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Is this slug used by another destination? exceptId = the one being edited, which may keep its own slug.</summary>
    /// <summary>Cheaper than GetByIdAsync when only "is it there?" matters - no photos are loaded.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken);

    Task<bool> CountryExistsAsync(short countryId, CancellationToken cancellationToken);

    /// <summary>Does any (not deleted) tour package still point at this destination?</summary>
    Task<bool> IsUsedByPackagesAsync(Guid destinationId, CancellationToken cancellationToken);

    void Add(Destination destination);
}
