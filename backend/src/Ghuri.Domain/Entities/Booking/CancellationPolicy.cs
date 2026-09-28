using Ghuri.Domain.Common;

namespace Ghuri.Domain.Entities.Booking;

/// <summary>
/// Refund % by days before departure (blueprint: booking.CancellationPolicies
/// [A]). Its own aggregate root - has its own repository
/// (ICancellationPolicyRepository), which falls back to the global policy
/// (PackageId = null) when a package has no specific rows of its own.
/// </summary>
public sealed class CancellationPolicy : AggregateRoot, IAuditable
{
    /// <summary>Null means this is the global default policy, used when a package has no rules of its own.</summary>
    public Guid? PackageId { get; private set; }

    public short MinDaysBefore { get; private set; }
    public decimal RefundPercent { get; private set; }

    private CancellationPolicy()
    {
    }

    public static CancellationPolicy Create(Guid? packageId, short minDaysBefore, decimal refundPercent)
    {
        if (refundPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(refundPercent), "RefundPercent must be between 0 and 100.");

        return new CancellationPolicy
        {
            PackageId = packageId,
            MinDaysBefore = minDaysBefore,
            RefundPercent = refundPercent
        };
    }
}
