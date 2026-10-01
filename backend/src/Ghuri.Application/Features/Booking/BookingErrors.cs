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
}
