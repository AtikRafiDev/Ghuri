namespace Ghuri.Domain.Entities.Marketing;

/// <summary>A saved package (blueprint: marketing.Wishlists). Pure join entity, composite PK, no surrogate id - same shape as iam.UserRole.</summary>
public sealed class Wishlist
{
    public Guid UserId { get; private set; }
    public Guid PackageId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Wishlist()
    {
    }

    public static Wishlist Create(Guid userId, Guid packageId, DateTime nowUtc) =>
        new() { UserId = userId, PackageId = packageId, CreatedAtUtc = nowUtc };
}
