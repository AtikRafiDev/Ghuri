using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Tests.Entities.Catalog;

public class DepartureTests
{
    private static readonly DateOnly Dec20 = new(2026, 12, 20);

    private static Departure NewDeparture(short totalSeats = 20) =>
        Departure.Create(Guid.NewGuid(), Dec20, durationDays: 3, 12_000, 9_000, 0, null, totalSeats, bookingCutoffDays: 2);

    /// <summary>
    /// Only the repository's atomic UPDATE changes ReservedSeats - there's no
    /// public setter on purpose. Tests set it directly to stand in for "some
    /// seats were booked".
    /// </summary>
    private static Departure WithBookedSeats(Departure departure, short seats)
    {
        typeof(Departure).GetProperty(nameof(Departure.ReservedSeats))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(departure, [seats]);
        return departure;
    }

    [Fact]
    public void Create_WorksOutTheEndDateFromTheDuration()
    {
        var departure = NewDeparture();

        Assert.Equal(new DateOnly(2026, 12, 22), departure.EndDate); // 20, 21, 22 = 3 days
        Assert.Equal(DepartureStatus.Open, departure.Status);
        Assert.Equal(20, departure.SeatsLeft);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Create_SeatsOutsideOneToAThousand_AreRejected(short seats) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NewDeparture(seats));

    [Fact]
    public void Create_FreeAdultPrice_IsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Departure.Create(Guid.NewGuid(), Dec20, 3, adultPrice: 0, 0, 0, null, 20, 0));

    [Fact]
    public void Update_WithBookedSeats_CannotMoveTheDate()
    {
        var departure = WithBookedSeats(NewDeparture(), 4);

        var error = Assert.Throws<DomainException>(
            () => departure.Update(Dec20.AddDays(7), 3, 12_000, 9_000, 0, null, 20, 2));

        Assert.Equal("departure_dates_locked", error.Code);
    }

    [Fact]
    public void Update_WithBookedSeats_CanStillChangePricesAndAddSeats()
    {
        var departure = WithBookedSeats(NewDeparture(), 4);

        departure.Update(Dec20, 3, 11_000, 8_500, 0, 3_000, 25, 2);

        Assert.Equal(11_000, departure.AdultPrice);
        Assert.Equal(21, departure.SeatsLeft);
    }

    [Fact]
    public void Update_FewerSeatsThanBooked_IsRejected()
    {
        var departure = WithBookedSeats(NewDeparture(), 10);

        var error = Assert.Throws<DomainException>(() => departure.Update(Dec20, 3, 12_000, 9_000, 0, null, 9, 2));

        Assert.Equal("departure_seats_below_reserved", error.Code);
    }

    [Fact]
    public void ChangeDuration_MovesTheEndDate_OnlyWhileNoSeatsAreBooked()
    {
        var departure = NewDeparture();
        departure.ChangeDuration(5);
        Assert.Equal(new DateOnly(2026, 12, 24), departure.EndDate);

        WithBookedSeats(departure, 1);
        Assert.Throws<DomainException>(() => departure.ChangeDuration(4));
    }

    [Fact]
    public void Close_OpenDeparture_StopsSelling_AndTwiceIsRejected()
    {
        var departure = NewDeparture();

        departure.Close();

        Assert.Equal(DepartureStatus.Closed, departure.Status);
        Assert.Equal("departure_not_open", Assert.Throws<DomainException>(departure.Close).Code);
    }

    // ---------- CheckBookable: departs 20 Dec, booking closes 2 days before ----------

    [Fact]
    public void LastBookingDate_IsCutoffDaysBeforeTheStart() =>
        Assert.Equal(new DateOnly(2026, 12, 18), NewDeparture().LastBookingDate);

    [Theory]
    [InlineData(2026, 12, 18, DepartureBookability.Bookable)]      // the last booking day itself still works
    [InlineData(2026, 12, 19, DepartureBookability.BookingClosed)] // one day later: closed
    [InlineData(2026, 12, 25, DepartureBookability.BookingClosed)] // already departed
    public void CheckBookable_ClosesAfterTheLastBookingDate(int year, int month, int day, DepartureBookability expected) =>
        Assert.Equal(expected, NewDeparture().CheckBookable(new DateOnly(year, month, day), seats: 1));

    [Fact]
    public void CheckBookable_ExactlyTheSeatsLeft_IsFine_OneMoreIsNot()
    {
        var departure = WithBookedSeats(NewDeparture(totalSeats: 20), 17); // 3 left
        var today = new DateOnly(2026, 12, 1);

        Assert.Equal(DepartureBookability.Bookable, departure.CheckBookable(today, seats: 3));
        Assert.Equal(DepartureBookability.NotEnoughSeats, departure.CheckBookable(today, seats: 4));
    }

    [Fact]
    public void CheckBookable_ClosedDeparture_IsNotOpen()
    {
        var departure = NewDeparture();
        departure.Close();

        Assert.Equal(DepartureBookability.NotOpen, departure.CheckBookable(new DateOnly(2026, 12, 1), seats: 1));
    }
}
