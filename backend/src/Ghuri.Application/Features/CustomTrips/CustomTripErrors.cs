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

    /// <summary>Kept next to the domain's limits so the messages and the rules never drift apart.</summary>
    public static readonly string LegsMessage = $"Add 1 to {CustomTrip.MaxLegs} destinations.";
}
