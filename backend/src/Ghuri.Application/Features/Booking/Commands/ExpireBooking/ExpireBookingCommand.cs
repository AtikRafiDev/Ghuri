using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Application.Features.CustomTrips;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Features.Booking.Commands.ExpireBooking;

/// <summary>
/// One unpaid booking's hold is over: mark it Expired and give its seats
/// back (17-day plan, Day 8: "ExpireUnpaidHolds - expire booking, release
/// seats for fixed departures"). Sent by the expiry job, one booking per
/// command, so each runs in its own transaction.
/// </summary>
public sealed record ExpireBookingCommand(Guid BookingId) : ICommand;

/// <remarks>
/// The booking change and the seat release share the transaction: both
/// happen or neither. If the customer's payment confirms the same booking
/// at this very moment, the booking's RowVersion makes one of the two
/// saves fail - this one rolls back, seats included, and the payment wins.
/// </remarks>
internal sealed class ExpireBookingHandler(
    IBookingRepository bookings,
    IDepartureRepository departures,
    CustomTripBookingSync customTrips,
    TimeProvider clock,
    ILogger<ExpireBookingHandler> logger) : ICommandHandler<ExpireBookingCommand>
{
    public async ValueTask<Result> Handle(ExpireBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(command.BookingId, cancellationToken);
        var nowUtc = clock.GetUtcNow().UtcDateTime;

        // Paid, cancelled or already expired since the job made its list, or
        // the hold isn't over after all: nothing to do - and not an error.
        if (booking is null || booking.Status != BookingStatus.PendingPayment || booking.HoldExpiresAtUtc > nowUtc)
            return Result.Success();

        booking.Expire(nowUtc);
        await customTrips.BookingEndedAsync(booking, nowUtc, cancellationToken); // a custom trip can be accepted again

        if (booking.SeatsHeld > 0
            && !await departures.ReleaseSeatsAsync(booking.DepartureId!.Value, booking.SeatsHeld, cancellationToken))
        {
            // ReleaseSeats refuses to go below zero, so the counts disagree
            // somewhere. The booking still expires; staff should look.
            logger.LogWarning(
                "Booking {BookingNo} expired, but its {Seats} seat(s) could not be released on departure {DepartureId}.",
                booking.BookingNo, booking.SeatsHeld, booking.DepartureId);
        }

        logger.LogInformation("Booking {BookingNo} expired unpaid; {Seats} seat(s) released.", booking.BookingNo, booking.SeatsHeld);
        return Result.Success();
    }
}
