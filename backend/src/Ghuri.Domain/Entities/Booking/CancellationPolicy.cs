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

    /// <summary>
    /// The refund % for cancelling <paramref name="daysBefore"/> days before
    /// the trip: the rule with the biggest MinDaysBefore that still fits.
    /// The package's own rules win; without any, the global ones (PackageId
    /// null) apply. No rule fits → 0%.
    /// </summary>
    /// <example>Rules 30→100%, 15→50%, 7→25%, 0→0%: 40 days → 100 · 20 days → 50 · 10 days → 25 · 3 days → 0.</example>
    public static decimal RefundPercentFor(IReadOnlyCollection<CancellationPolicy> rules, Guid? packageId, int daysBefore)
    {
        ArgumentNullException.ThrowIfNull(rules);

        List<CancellationPolicy> own = packageId is null ? [] : rules.Where(r => r.PackageId == packageId).ToList();
        var applicable = own.Count > 0 ? own : rules.Where(r => r.PackageId is null).ToList();

        return applicable
            .Where(r => r.MinDaysBefore <= daysBefore)
            .OrderByDescending(r => r.MinDaysBefore)
            .Select(r => r.RefundPercent)
            .FirstOrDefault();
    }

    public static CancellationPolicy Create(Guid? packageId, short minDaysBefore, decimal refundPercent)
    {
        if (refundPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(refundPercent), "RefundPercent must be between 0 and 100.");
        if (minDaysBefore < 0)
            throw new ArgumentOutOfRangeException(nameof(minDaysBefore), "MinDaysBefore can't be negative.");

        return new CancellationPolicy
        {
            PackageId = packageId,
            MinDaysBefore = minDaysBefore,
            RefundPercent = refundPercent
        };
    }
}
