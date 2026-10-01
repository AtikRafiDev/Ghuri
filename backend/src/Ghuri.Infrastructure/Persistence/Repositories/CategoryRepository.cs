using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of ICategoryRepository.</summary>
internal sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    // SQL Server's default collation ignores case: "beach" matches "Beach".
    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        db.Categories.AnyAsync(c => c.Name == name && c.Id != exceptId, cancellationToken);

    public Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken) =>
        db.Categories.AnyAsync(c => c.Slug == slug && c.Id != exceptId, cancellationToken);

    public Task<bool> IsUsedByPackagesAsync(Guid categoryId, CancellationToken cancellationToken) =>
        db.PackageCategories.AnyAsync(
            pc => pc.CategoryId == categoryId && db.TourPackages.Any(p => p.Id == pc.PackageId),
            cancellationToken);

    public async Task<bool> AllExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return true;
        var distinct = ids.Distinct().ToList();
        // One query for the whole list; the soft-delete filter means a deleted category doesn't count.
        var found = await db.Categories.CountAsync(c => distinct.Contains(c.Id), cancellationToken);
        return found == distinct.Count;
    }

    public void Add(Category category) => db.Categories.Add(category);
}
