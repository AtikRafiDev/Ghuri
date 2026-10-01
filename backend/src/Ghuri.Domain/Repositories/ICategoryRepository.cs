using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Repositories;

/// <summary>Tour categories (blueprint section 7.1). Deleted categories are invisible to every method.</summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>exceptId = the category being edited, which may keep its own name/slug.</summary>
    Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>Is any (not deleted) tour package tagged with this category?</summary>
    Task<bool> IsUsedByPackagesAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>Lets the package handlers reject unknown (or deleted) category ids, instead of a database FK error.</summary>
    Task<bool> AllExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    void Add(Category category);
}
