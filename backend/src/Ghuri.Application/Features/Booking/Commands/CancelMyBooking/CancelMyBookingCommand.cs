using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Features.Booking.Commands.CancelMyBooking;

/// <summary>
/// The customer cancels one of their own bookings (17-day plan, Day 11:
/// CancelBookingByCustomer). The booking becomes Cancelled and its seats go
/// back. If it was paid, a Refund is requested for the amount the
/// cancellation policy allows - staff pay it out and mark it refunded (Day 12).
/// </summary>
/// <param name="BookingNo">One of the customer's own bookings, e.g. TB100001.</param>
/// <param name="Reason">Optional - "Cancelled by the customer." when empty.</param>
public sealed record CancelMyBookingCommand(string BookingNo, string? Reason) : ICommand<CancelMyBookingResponse>;

/// <summary>RefundNo is null when nothing is refunded (unpaid, or 0% this close to the trip).</summary>
public sealed record CancelMyBookingResponse(string BookingNo, decimal RefundPercent, decimal RefundAmount, string? RefundNo);

/// <remarks>
/// The booking row is LOCKED first: if the payment confirmation runs at the
/// same moment, one waits for the other - never a booking that is both
/// cancelled and confirmed. The refund is worked out by CancellationTerms,
/// the same code that showed the customer the amount beforehand.
/// </remarks>
internal sealed class CancelMyBookingHandler(
    IBookingRepository bookings,
    IDepartureRepository departures,
    IRefundRepository refunds,
    IReadDbContext db,
    CancellationTerms terms,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<CancelMyBookingHandler> logger) : ICommandHandler<CancelMyBookingCommand, CancelMyBookingResponse>
{
    private const string DefaultReason = "Cancelled by the customer.";

    public async ValueTask<Result<CancelMyBookingResponse>> Handle(CancelMyBookingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        // Someone else's booking is "not found", like GetMyBooking.
        var booking = await bookings.GetByBookingNoForUpdateAsync(command.BookingNo, cancellationToken);
        if (booking is null || booking.CustomerId != customerId)
            return BookingErrors.BookingNotFound;

        var quote = await terms.QuoteAsync(booking.Status, booking.PackageId, booking.StartDate, booking.PaidAmount, cancellationToken);
        if (!quote.CanCancel)
            return BookingErrors.NotCancellable(quote.Reason!);

        var reason = string.IsNullOrWhiteSpace(command.Reason) ? DefaultReason : command.Reason.Trim();
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        booking.Cancel(nowUtc, reason, customerId);

        if (booking.SeatsHeld > 0
            && !await departures.ReleaseSeatsAsync(booking.DepartureId!.Value, booking.SeatsHeld, cancellationToken))
        {
            // Same as ExpireBookingHandler: the counts disagree somewhere. The booking is still cancelled; staff should look.
            logger.LogWarning(
                "Booking {BookingNo} cancelled, but its {Seats} seat(s) could not be released on departure {DepartureId}.",
                booking.BookingNo, booking.SeatsHeld, booking.DepartureId);
        }

        string? refundNo = null;
        if (quote.RefundAmount > 0)
        {
            // The money came in through this payment, so it goes back through it.
            var paymentId = await db.Payments
                .Where(p => p.BookingId == booking.Id && p.Status == PaymentStatus.Succeeded)
                .OrderByDescending(p => p.PaidAtUtc)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Booking {booking.BookingNo} is paid but has no succeeded payment.");

            refundNo = await refunds.NextRefundNoAsync(cancellationToken);
            refunds.Add(Refund.Request(refundNo, booking.Id, paymentId, quote.RefundAmount, quote.RefundPercent, reason, customerId));
        }

        logger.LogInformation(
            "Customer cancelled booking {BookingNo}; refund {RefundAmount} ({RefundPercent}%).",
            booking.BookingNo, quote.RefundAmount, quote.RefundPercent);
        return new CancelMyBookingResponse(booking.BookingNo, quote.RefundPercent, quote.RefundAmount, refundNo);
    }
}
