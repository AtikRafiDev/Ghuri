namespace Ghuri.Domain.Enums;

/// <summary>
/// Maps to booking.CustomTrips.Status (TINYINT) - the custom trip's life
/// (17-day plan, Day 13): Submitted → Quoted → Accepted → Paid, or
/// Rejected / Expired / Cancelled along the way. See CustomTrip.
/// </summary>
public enum CustomTripStatus : byte
{
    /// <summary>The customer sent the request; staff haven't quoted yet.</summary>
    Submitted = 1,

    /// <summary>Staff sent a price; the customer can accept until QuoteExpiresAtUtc.</summary>
    Quoted = 2,

    /// <summary>The customer accepted - a booking waits for payment (Day 15).</summary>
    Accepted = 3,

    /// <summary>Paid - the trip is on (Day 15).</summary>
    Paid = 4,

    /// <summary>Staff can't do it (no hotels, unsafe season…) - RejectReason says why.</summary>
    Rejected = 5,

    /// <summary>The quote ran out before the customer accepted. Staff can quote again.</summary>
    Expired = 6,

    /// <summary>The customer withdrew it before paying.</summary>
    Cancelled = 7
}
