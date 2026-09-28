namespace Ghuri.Domain.Enums;

/// <summary>Maps to booking.Bookings.Status (TINYINT). See the blueprint's booking state machine, section 6.3.</summary>
public enum BookingStatus : byte
{
    PendingPayment = 1,
    Confirmed = 2,
    PartiallyPaid = 3,
    Completed = 4,
    Cancelled = 5,
    Expired = 6
}
