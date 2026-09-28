namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// Many-to-many packages ⇄ categories. Unlike PackageImage/ItineraryDay/
/// PackageAddOn, this Create is public - Category is its own independent
/// aggregate (it has its own ICategoryRepository per the blueprint), so
/// this join isn't "owned" by either side the way an image is owned by
/// its package. Same shape as iam.UserRole.
/// </summary>
public sealed class PackageCategory
{
    public Guid PackageId { get; private set; }
    public Guid CategoryId { get; private set; }

    private PackageCategory()
    {
    }

    public static PackageCategory Create(Guid packageId, Guid categoryId) =>
        new() { PackageId = packageId, CategoryId = categoryId };
}
