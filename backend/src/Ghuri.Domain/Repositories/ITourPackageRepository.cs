using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Repositories;

/// <summary>Tour packages (blueprint section 7.1). Deleted packages are invisible to every method.</summary>
public interface ITourPackageRepository
{
    /// <summary>The whole aggregate: photos, itinerary days and category links come with it.</summary>
    Task<TourPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// A package that's for sale, by its URL name - for booking it. Just the
    /// package row (no photos or itinerary): booking needs its prices and
    /// rules, nothing else. Null for a draft, archived or unknown package.
    /// </summary>
    Task<TourPackage?> GetPublishedBySlugAsync(Slug slug, CancellationToken cancellationToken);

    /// <summary>Is this slug used by another package? exceptId = the one being edited, which may keep its own slug.</summary>
    Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>The next free code - PKG1001, PKG1002... - taken from a database SEQUENCE, so two admins never get the same one.</summary>
    Task<string> NextPackageCodeAsync(CancellationToken cancellationToken);

    void Add(TourPackage package);
}
