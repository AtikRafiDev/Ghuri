using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Repositories;

/// <summary>Destinations (blueprint section 7.1). Deleted destinations are invisible to every method.</summary>
public interface IDestinationRepository
{
    Task<Destination?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Cheaper than GetByIdAsync when only "is it there?" matters - no photos are loaded.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Is this slug used by another destination? exceptId = the one being edited, which may keep its own slug.</summary>
    Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken);

    Task<bool> CountryExistsAsync(short countryId, CancellationToken cancellationToken);

    /// <summary>Does any (not deleted) tour package still point at this destination?</summary>
    Task<bool> IsUsedByPackagesAsync(Guid destinationId, CancellationToken cancellationToken);

    /// <summary>
    /// The destinations on this sort order or above, lowest first, tracked so
    /// DisplayOrder.MakeRoom can move them along. exceptId = the one being
    /// saved. No photos are loaded - only SortOrder changes.
    /// </summary>
    Task<IReadOnlyList<Destination>> GetFromSortOrderAsync(int sortOrder, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>The highest sort order in use (null = no destinations), leaving out exceptId.</summary>
    Task<int?> GetLastSortOrderAsync(Guid? exceptId, CancellationToken cancellationToken);

    void Add(Destination destination);
}
