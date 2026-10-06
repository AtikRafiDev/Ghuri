using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Booking;

/// <summary>
/// Every expected failure when pricing or booking a trip. Codes never change
/// once shipped. Shared by the quote (Day 6) and the booking (Day 8).
/// </summary>
public static class BookingErrors
{
    public static readonly Error PackageNotFound =
        Error.NotFound("package_not_found", "This package does not exist or is no longer for sale.");

    // Fixed departures
    public static readonly Error DepartureRequired =
        Error.Failure("departure_required", "Choose a departure date.");

    public static readonly Error DepartureNotFound =
        Error.NotFound("departure_not_found", "This departure date does not exist.");

    public static readonly Error DepartureNotBookable =
        Error.Conflict("departure_not_bookable", "This date is no longer taking bookings. Please choose another date.");

    public static Error BookingClosed(DateOnly lastBookingDate) =>
        Error.Conflict("booking_closed", $"Booking for this date closed on {lastBookingDate:d MMM yyyy}. Please choose a later date.");

    public static Error NotEnoughSeats(int seatsLeft) =>
        Error.Conflict(
            "not_enough_seats",
            seatsLeft == 0 ? "This date is sold out." : $"Only {seatsLeft} seat(s) left on this date.");

    public static readonly Error SingleRoomNotOffered =
        Error.Failure("single_room_not_offered", "Single rooms aren't offered for this trip.");

    // Flexible stays
    public static readonly Error FlexibleDatesRequired =
        Error.Failure("flexible_dates_required", "Choose a start date and the number of nights.");

    public static Error NightsOutOfRange(int minNights, int maxNights) =>
        Error.Failure("nights_out_of_range", $"Choose {minNights} to {maxNights} nights.");

    public static Error StartDateTooSoon(DateOnly earliest) =>
        Error.Failure("start_date_too_soon", $"The earliest start date is {earliest:d MMM yyyy}.");

    // Creating a booking (Day 8)

    /// <summary>
    /// The seats were free when we looked, but someone else's booking took
    /// them a moment before ours (the atomic reservation said no). Same code
    /// as NotEnoughSeats, so the checkout handles both the same way.
    /// </summary>
    public static readonly Error SeatsJustTaken =
        Error.Conflict("not_enough_seats", "Someone has just booked the last seats on this date. Please choose another date or fewer travellers.");

    /// <summary>The same Idempotency-Key came back with a DIFFERENT request (or from another user) - a client bug, not a retry.</summary>
    public static readonly Error IdempotencyKeyReused =
        Error.Conflict("idempotency_key_reused", "This Idempotency-Key was already used for a different request. Send a new key.");

    public static readonly Error BookingNotFound =
        Error.NotFound("booking_not_found", "This booking doesn't exist.");

    // Cancelling (Day 11)

    /// <summary>Expired, already cancelled, or the trip has started - Message says which (from CancellationQuote.Reason).</summary>
    public static Error NotCancellable(string reason) =>
        Error.Conflict("booking_not_cancellable", reason);

    /// <summary>The invoice before anything is paid, or the voucher of a booking that isn't confirmed.</summary>
    public static Error DocumentNotAvailable(string message) =>
        Error.Conflict("document_not_available", message);
}
