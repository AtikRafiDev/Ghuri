using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>An optional extra (airport pickup, single room...). Owned by TourPackage - see PackageImage's remarks.</summary>
public sealed class PackageAddOn : BaseEntity, IAuditable
{
    public Guid PackageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public PricingUnit PricingUnit { get; private set; }
    public bool IsActive { get; private set; }

    private PackageAddOn()
    {
    }

    internal static PackageAddOn Create(Guid packageId, string name, decimal price, PricingUnit pricingUnit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");

        return new PackageAddOn
        {
            PackageId = packageId,
            Name = name,
            Price = price,
            PricingUnit = pricingUnit,
            IsActive = true
        };
    }
}
