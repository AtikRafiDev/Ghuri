using Ghuri.Domain.Common;

namespace Ghuri.Domain.Entities.Marketing;

/// <summary>One use of a coupon. A standalone entity (not owned by Coupon) since it also references a Booking and a User.</summary>
public sealed class CouponRedemption : BaseEntity
{
    public Guid CouponId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public DateTime RedeemedAtUtc { get; private set; }

    private CouponRedemption()
    {
    }

    public static CouponRedemption Create(Guid couponId, Guid bookingId, Guid userId, decimal discountAmount, DateTime nowUtc) =>
        new()
        {
            CouponId = couponId,
            BookingId = bookingId,
            UserId = userId,
            DiscountAmount = discountAmount,
            RedeemedAtUtc = nowUtc
        };
}
