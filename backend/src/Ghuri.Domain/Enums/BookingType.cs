namespace Ghuri.Domain.Enums;

/// <summary>
/// Maps to booking.Bookings.BookingType (TINYINT). What was booked - it
/// decides which of DepartureId / PackageId / CustomTripId must be set.
/// </summary>
public enum BookingType : byte
{
    /// <summary>A seat on a fixed departure: DepartureId and PackageId set.</summary>
    FixedDeparture = 1,

    /// <summary>A flexible stay - the customer's own start date and nights: PackageId set, no departure.</summary>
    FlexibleStay = 2,

    /// <summary>An accepted custom-trip quote (17-day plan, Day 15): CustomTripId set, no package or departure.</summary>
    CustomTrip = 3
}
