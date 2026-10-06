using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking;

/// <summary>
/// What cancelling this booking right now would mean. CanCancel false →
/// Reason says why. RefundAmount is from the money actually paid, by the
/// cancellation policy (Day 11: "refund from global policy, days before
/// StartDate").
/// </summary>
public sealed record CancellationQuote(bool CanCancel, int DaysBeforeStart, decimal RefundPercent, decimal RefundAmount, string? Reason)
{
    public static CancellationQuote Refused(int daysBeforeStart, string reason) => new(false, daysBeforeStart, 0, 0, reason);
}

/// <summary>
/// Works out a CancellationQuote. Shared by the booking page (to show "you'd
/// get back ৳X" BEFORE the customer decides) and the cancel command (to
/// refund exactly that), so the two can never disagree.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Unpaid (PendingPayment): can cancel, nothing to refund - the seats just go back.</item>
/// <item>Confirmed: can cancel until the day before the trip; the refund % comes from
/// the policy rows (the package's own, else the global ones - CancellationPolicy.RefundPercentFor).</item>
/// <item>Anything else (expired, already cancelled, completed): can't.</item>
/// </list>
/// "Days before" counts in Bangladesh dates: cancelling on 17 Dec for a
/// trip starting 20 Dec is 3 days before, whatever the hour.
/// </remarks>
internal sealed class CancellationTerms(IReadDbContext db, TimeProvider clock)
{
    public async Task<CancellationQuote> QuoteAsync(
        BookingStatus status, Guid? packageId, DateOnly startDate, decimal paidAmount, CancellationToken cancellationToken)
    {
        var daysBefore = startDate.DayNumber - clock.Today().DayNumber;

        if (status is not (BookingStatus.PendingPayment or BookingStatus.Confirmed))
            return CancellationQuote.Refused(daysBefore, "This booking can no longer be cancelled.");
        if (daysBefore <= 0)
            return CancellationQuote.Refused(daysBefore, "The trip has already started. Please contact us.");
        if (paidAmount <= 0)
            return new CancellationQuote(true, daysBefore, 0, 0, null);

        var rules = await db.CancellationPolicies
            .Where(r => r.PackageId == null || r.PackageId == packageId)
            .ToListAsync(cancellationToken);
        var percent = CancellationPolicy.RefundPercentFor(rules, packageId, daysBefore);

        // Whole paisa, rounded half up: ৳33,333.335 → ৳33,333.34.
        var amount = Math.Round(paidAmount * percent / 100m, 2, MidpointRounding.AwayFromZero);
        return new CancellationQuote(true, daysBefore, percent, amount, null);
    }
}
