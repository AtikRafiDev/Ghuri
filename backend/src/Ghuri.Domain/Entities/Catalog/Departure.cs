using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// A dated run of a package, with its own price and seat count
/// (blueprint: catalog.Departures [A]). Its own aggregate root - it has
/// its own repository (IDepartureRepository) because seat reservation
/// needs to be atomic at the database level (an ExecuteUpdateAsync
/// compare-and-set, shown in the blueprint section 7.3), which only makes
/// sense as a repository method, not something this loaded-into-memory
/// object could do safely itself.
/// </summary>
/// <remarks>
/// Uses DateOnly (not DateTime) for StartDate/EndDate, matching the
/// blueprint's plain DATE column type. EndDate is never typed by staff: it
/// is worked out from the package's duration, so a 3-day package can't get
/// a 5-day departure.
/// </remarks>
public sealed class Departure : AggregateRoot, IAuditable
{
    /// <summary>A coach or a small hotel block - plenty; keeps a typo like 2000 out.</summary>
    public const short MaxSeats = 1000;

    public Guid PackageId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal AdultPrice { get; private set; }
    public decimal ChildPrice { get; private set; }
    public decimal InfantPrice { get; private set; }
    public decimal? SingleSupplement { get; private set; }
    public short TotalSeats { get; private set; }

    /// <summary>Held + confirmed seats combined; changed only by an atomic UPDATE in the repository, never directly here.</summary>
    public short ReservedSeats { get; private set; }

    /// <summary>Booking closes this many days before StartDate (time to arrange transport and hotels).</summary>
    public byte BookingCutoffDays { get; private set; }

    public DepartureStatus Status { get; private set; }

    public int SeatsLeft => TotalSeats - ReservedSeats;

    private Departure()
    {
    }

    /// <summary>A new, open departure. durationDays = the package's DurationDays (EndDate = StartDate + durationDays - 1).</summary>
    public static Departure Create(
        Guid packageId, DateOnly startDate, byte durationDays,
        decimal adultPrice, decimal childPrice, decimal infantPrice, decimal? singleSupplement,
        short totalSeats, byte bookingCutoffDays)
    {
        var departure = new Departure { PackageId = packageId, ReservedSeats = 0, Status = DepartureStatus.Open };
        departure.Apply(startDate, durationDays, adultPrice, childPrice, infantPrice, singleSupplement, totalSeats, bookingCutoffDays);
        return departure;
    }

    /// <summary>
    /// Replaces the editable fields. Two rules protect customers who already
    /// hold seats: the dates can't move, and the seat count can't drop below
    /// the seats already taken.
    /// </summary>
    public void Update(
        DateOnly startDate, byte durationDays,
        decimal adultPrice, decimal childPrice, decimal infantPrice, decimal? singleSupplement,
        short totalSeats, byte bookingCutoffDays)
    {
        if (Status is DepartureStatus.Cancelled or DepartureStatus.Completed)
            throw new DomainException("departure_not_editable", "A cancelled or completed departure can't be changed.");
        if (ReservedSeats > 0 && startDate != StartDate)
            throw new DomainException("departure_dates_locked", "Seats are already booked - the date can't be moved.");
        if (totalSeats < ReservedSeats)
            throw new DomainException(
                "departure_seats_below_reserved", $"{ReservedSeats} seats are already booked - total seats can't be lower.");

        Apply(startDate, durationDays, adultPrice, childPrice, infantPrice, singleSupplement, totalSeats, bookingCutoffDays);
    }

    /// <summary>
    /// The package's duration changed: move EndDate with it. Refused once
    /// seats are booked - those customers were sold the old dates.
    /// </summary>
    public void ChangeDuration(byte durationDays)
    {
        if (durationDays < 1)
            throw new ArgumentOutOfRangeException(nameof(durationDays), "Duration must be at least 1 day.");
        if (ReservedSeats > 0)
            throw new DomainException("departure_dates_locked", "Seats are already booked - the dates can't change.");

        EndDate = StartDate.AddDays(durationDays - 1);
    }

    /// <summary>No new bookings; existing bookings keep their seats. Used when the trip is full or no longer sold.</summary>
    public void Close()
    {
        if (Status != DepartureStatus.Open)
            throw new DomainException("departure_not_open", "Only an open departure can be closed.");

        Status = DepartureStatus.Closed;
    }

    private void Apply(
        DateOnly startDate, byte durationDays,
        decimal adultPrice, decimal childPrice, decimal infantPrice, decimal? singleSupplement,
        short totalSeats, byte bookingCutoffDays)
    {
        if (durationDays < 1)
            throw new ArgumentOutOfRangeException(nameof(durationDays), "Duration must be at least 1 day.");
        if (totalSeats is <= 0 or > MaxSeats)
            throw new ArgumentOutOfRangeException(nameof(totalSeats), $"Total seats must be between 1 and {MaxSeats}.");
        if (adultPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(adultPrice), "The adult price must be more than zero.");
        if (childPrice < 0 || infantPrice < 0 || singleSupplement < 0)
            throw new ArgumentOutOfRangeException(nameof(childPrice), "Prices cannot be negative.");

        StartDate = startDate;
        EndDate = startDate.AddDays(durationDays - 1);
        AdultPrice = adultPrice;
        ChildPrice = childPrice;
        InfantPrice = infantPrice;
        SingleSupplement = singleSupplement;
        TotalSeats = totalSeats;
        BookingCutoffDays = bookingCutoffDays;
    }
}
