using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// A dated run of a package, with its own price and seat count
/// (blueprint: catalog.Departures [A]). Its own aggregate root - it has
/// its own repository (IDepartureRepository) because seat reservation
/// needs to be atomic at the database level (an ExecuteUpdateAsync
/// compare-and-set, shown in the blueprint section 7.3), which only makes
/// sense as a repository method, not something this loaded-into-memory
/// object could do safely itself. That reservation logic is Day 5 work.
/// </summary>
/// <remarks>Uses DateOnly (not DateTime) for StartDate/EndDate, matching the blueprint's plain DATE column type.</remarks>
public sealed class Departure : AggregateRoot, IAuditable
{
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

    public byte BookingCutoffDays { get; private set; }
    public DepartureStatus Status { get; private set; }

    private Departure()
    {
    }

    public static Departure Create(
        Guid packageId, DateOnly startDate, DateOnly endDate,
        decimal adultPrice, decimal childPrice, decimal infantPrice,
        short totalSeats, byte bookingCutoffDays, decimal? singleSupplement = null)
    {
        if (endDate < startDate)
            throw new ArgumentException("EndDate must be on or after StartDate.", nameof(endDate));
        if (totalSeats <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalSeats), "TotalSeats must be greater than zero.");
        if (adultPrice < 0 || childPrice < 0 || infantPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(adultPrice), "Prices cannot be negative.");

        return new Departure
        {
            PackageId = packageId,
            StartDate = startDate,
            EndDate = endDate,
            AdultPrice = adultPrice,
            ChildPrice = childPrice,
            InfantPrice = infantPrice,
            SingleSupplement = singleSupplement,
            TotalSeats = totalSeats,
            ReservedSeats = 0,
            BookingCutoffDays = bookingCutoffDays,
            Status = DepartureStatus.Open
        };
    }
}
