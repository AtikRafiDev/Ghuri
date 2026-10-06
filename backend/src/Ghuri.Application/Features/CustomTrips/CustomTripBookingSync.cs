using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Application.Features.CustomTrips;

/// <summary>
/// Keeps a custom trip in step with the booking made from its quote (Day 15).
/// Called by the commands that end a booking - the expiry job, the
/// customer's cancel, the agency's cancel - inside their own transaction, so
/// the booking and its trip change together:
/// <list type="bullet">
/// <item>booking expired unpaid → trip back to Quoted (or Expired if the offer ran out) - the customer can accept again;</item>
/// <item>booking cancelled → trip Cancelled too.</item>
/// </list>
/// (Paid is the other direction: the payment confirmation raises
/// BookingConfirmed, and MarkCustomTripPaid handles it through the outbox.)
/// </summary>
internal sealed class CustomTripBookingSync(ICustomTripRepository trips)
{
    public async Task BookingEndedAsync(BookingEntity booking, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (booking.CustomTripId is not { } tripId)
            return; // a package booking - nothing to keep in step

        var trip = await trips.GetByIdForUpdateAsync(tripId, cancellationToken);
        if (trip is null)
            return;

        if (booking.Status == BookingStatus.Expired)
            trip.ReleaseAcceptance(nowUtc);
        else if (booking.Status == BookingStatus.Cancelled)
            trip.CancelWithBooking($"Booking {booking.BookingNo} was cancelled: {booking.CancelReason}", nowUtc);
    }
}
