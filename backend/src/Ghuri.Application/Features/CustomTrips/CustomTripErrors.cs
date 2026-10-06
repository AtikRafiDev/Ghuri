using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Booking;

namespace Ghuri.Application.Features.CustomTrips;

/// <summary>Every expected failure around custom trips (Day 13). Codes never change once shipped.</summary>
public static class CustomTripErrors
{
    public static readonly Error TripNotFound =
        Error.NotFound("custom_trip_not_found", "This trip request doesn't exist.");

    public static Error StartTooSoon(DateOnly earliest) =>
        Error.Failure("trip_start_too_soon", $"A custom trip can start on {earliest:d MMM yyyy} at the earliest - we need time to plan it.");

    /// <summary>A destination id that doesn't exist (or was deleted) - usually an old browser tab.</summary>
    public static readonly Error DestinationNotFound =
        Error.Failure("destination_not_found", "One of the destinations no longer exists. Please choose again.");

    public static readonly Error NotQuotable =
        Error.Conflict("trip_not_quotable", "This request can't be quoted any more - it was accepted, paid, rejected or cancelled.");

    public static readonly Error NotRejectable =
        Error.Conflict("trip_not_rejectable", "This request can no longer be rejected.");

    public static readonly Error NotCancellable =
        Error.Conflict("trip_not_cancellable", "This request can no longer be cancelled here. If you already accepted the quote, cancel the booking instead.");

    // Accepting (Day 15)

    public static readonly Error QuoteExpired =
        Error.Conflict("quote_expired", "This quote has run out. Please ask us for a new price.");

    public static readonly Error NotAcceptable =
        Error.Conflict("trip_not_acceptable", "There is no quote to accept on this trip.");

    /// <summary>The names sent must be exactly the people the quote was priced for.</summary>
    public static Error TravellersMismatch(CustomTrip trip) =>
        Error.Failure(
            "travellers_mismatch",
            $"This quote is for {trip.Adults} adult(s), {trip.Children} child(ren) and {trip.Infants} infant(s), with an adult as the lead - please enter exactly those travellers.");

    /// <summary>Kept next to the domain's limits so the messages and the rules never drift apart.</summary>
    public static readonly string LegsMessage = $"Add 1 to {CustomTrip.MaxLegs} destinations.";
}
