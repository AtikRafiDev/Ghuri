using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Marketing;

/// <summary>A discount code (blueprint: marketing.Coupons [A][S]). Its own aggregate root - has its own repository.</summary>
/// <remarks>IsApplicable(order, user, now) - the actual eligibility check - is Day 8/11 work, built with the booking/coupon-validation feature it serves.</remarks>
public sealed class Coupon : AggregateRoot, IAuditable, ISoftDeletable
{
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal Value { get; private set; }

    /// <summary>Caps how much a percent-based coupon can discount - ignored for Fixed discounts.</summary>
    public decimal? MaxDiscount { get; private set; }

    public decimal MinOrderAmount { get; private set; }
    public DateTime ValidFromUtc { get; private set; }
    public DateTime ValidToUtc { get; private set; }
    public int? MaxUses { get; private set; }
    public int MaxUsesPerUser { get; private set; }
    public int UsedCount { get; private set; }

    /// <summary>Null means the coupon applies to every package.</summary>
    public Guid? PackageId { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private Coupon()
    {
    }

    public static Coupon Create(
        string code, DiscountType discountType, decimal value, decimal minOrderAmount,
        DateTime validFromUtc, DateTime validToUtc, decimal? maxDiscount = null,
        int? maxUses = null, int maxUsesPerUser = 1, Guid? packageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be greater than zero.");
        if (validToUtc <= validFromUtc)
            throw new ArgumentException("ValidToUtc must be after ValidFromUtc.", nameof(validToUtc));

        return new Coupon
        {
            Code = code.ToUpperInvariant(),
            DiscountType = discountType,
            Value = value,
            MaxDiscount = maxDiscount,
            MinOrderAmount = minOrderAmount,
            ValidFromUtc = validFromUtc,
            ValidToUtc = validToUtc,
            MaxUses = maxUses,
            MaxUsesPerUser = maxUsesPerUser,
            UsedCount = 0,
            PackageId = packageId,
            IsActive = true
        };
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }
}
