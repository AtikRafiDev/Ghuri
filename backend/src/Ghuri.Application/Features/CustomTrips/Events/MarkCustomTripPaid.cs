using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Features.CustomTrips.Events;

/// <summary>
/// BookingConfirmed → if the booking came from a custom-trip quote, the trip
/// is Paid (17-day plan, Day 15: "IPN marks the trip Paid"). Done here, in
/// the outbox, so the payment code (IPN, manual payment) stays exactly as it
/// is - any confirmed booking of type CustomTrip lands here a few seconds later.
/// </summary>
/// <remarks>
/// Safe to run twice (the outbox is "at least once"): MarkPaid on a paid
/// trip does nothing. The outbox job saves the change together with marking
/// the event done.
/// </remarks>
internal sealed class MarkCustomTripPaid(
    IReadDbContext db,
    ICustomTripRepository trips,
    TimeProvider clock,
    ILogger<MarkCustomTripPaid> logger) : IDomainEventHandler<BookingConfirmed>
{
    public async Task HandleAsync(BookingConfirmed domainEvent, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .Where(b => b.Id == domainEvent.BookingId)
            .Select(b => new { b.BookingNo, b.CustomTripId })
            .FirstOrDefaultAsync(cancellationToken);
        if (booking?.CustomTripId is not { } tripId)
            return; // a package booking

        var trip = await trips.GetByIdForUpdateAsync(tripId, cancellationToken);
        if (trip is null)
            return;

        if (!trip.MarkPaid(clock.GetUtcNow().UtcDateTime))
        {
            // Paid for a trip that was cancelled or rejected meanwhile - the money is on the booking; staff decide.
            logger.LogWarning(
                "Booking {BookingNo} is paid, but its custom trip {TripNo} is {Status} - please check with the customer.",
                booking.BookingNo, trip.TripNo, trip.Status);
        }
    }
}
