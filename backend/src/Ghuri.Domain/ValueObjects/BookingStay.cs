using Ghuri.Domain.Enums;

namespace Ghuri.Domain.ValueObjects;

/// <summary>
/// WHAT a booking is for and WHEN: the type, the one link that type needs
/// (departure / package / custom trip) and the trip's dates. Built only
/// through the three named methods below, so a booking can't exist in an
/// impossible shape - e.g. a flexible stay that also points at a departure.
/// </summary>
/// <remarks>
/// The database's CK_Bookings_Shape and CK_Bookings_Dates check the same
/// rules, as a second line of defence.
/// </remarks>
public sealed record BookingStay
{
    public BookingType Type { get; }
    public Guid? DepartureId { get; }
    public Guid? PackageId { get; }
    public Guid? CustomTripId { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }
    public byte Nights { get; }

    private BookingStay(
        BookingType type, Guid? departureId, Guid? packageId, Guid? customTripId,
        DateOnly startDate, DateOnly endDate, byte nights)
    {
        if (endDate < startDate)
            throw new ArgumentException("A trip can't end before it starts.", nameof(endDate));

        Type = type;
        DepartureId = departureId;
        PackageId = packageId;
        CustomTripId = customTripId;
        StartDate = startDate;
        EndDate = endDate;
        Nights = nights;
    }

    /// <summary>A seat on a fixed departure. Dates and nights are copied from the departure and its package.</summary>
    public static BookingStay ForDeparture(Guid departureId, Guid packageId, DateOnly startDate, DateOnly endDate, byte nights) =>
        new(BookingType.FixedDeparture, Required(departureId, nameof(departureId)), Required(packageId, nameof(packageId)),
            null, startDate, endDate, nights);

    /// <summary>A flexible stay: the customer's check-in day and nights. Check-out is worked out here (2 nights from the 20th = out on the 22nd).</summary>
    public static BookingStay Flexible(Guid packageId, DateOnly checkIn, byte nights)
    {
        if (nights < 1)
            throw new ArgumentOutOfRangeException(nameof(nights), "A flexible stay needs at least one night.");

        return new(BookingType.FlexibleStay, null, Required(packageId, nameof(packageId)), null,
            checkIn, checkIn.AddDays(nights), nights);
    }

    /// <summary>An accepted custom-trip quote (Day 15) - no package, no departure.</summary>
    public static BookingStay ForCustomTrip(Guid customTripId, DateOnly startDate, DateOnly endDate, byte nights) =>
        new(BookingType.CustomTrip, null, null, Required(customTripId, nameof(customTripId)), startDate, endDate, nights);

    // Guid.Empty is never a real id - it means the caller forgot to set it.
    private static Guid Required(Guid id, string name) =>
        id == Guid.Empty ? throw new ArgumentException("An id is required.", name) : id;
}
