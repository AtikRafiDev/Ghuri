namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// Many-to-many packages ⇄ categories. The link belongs to the package -
/// it's changed only through TourPackage.SetCategories, so Create is
/// internal like PackageImage's. Category itself stays its own
/// independent aggregate: deleting a link never touches the category.
/// </summary>
public sealed class PackageCategory
{
    public Guid PackageId { get; private set; }
    public Guid CategoryId { get; private set; }

    private PackageCategory()
    {
    }

    internal static PackageCategory Create(Guid packageId, Guid categoryId) =>
        new() { PackageId = packageId, CategoryId = categoryId };
}
