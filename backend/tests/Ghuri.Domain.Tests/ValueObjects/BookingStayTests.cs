using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.ValueObjects;

public class BookingStayTests
{
    private static readonly DateOnly Dec20 = new(2026, 12, 20);

    [Fact]
    public void ForDeparture_LinksTheDepartureAndPackage_AndKeepsTheirDates()
    {
        var departureId = Guid.NewGuid();
        var packageId = Guid.NewGuid();

        var stay = BookingStay.ForDeparture(departureId, packageId, Dec20, Dec20.AddDays(2), nights: 2);

        Assert.Equal(BookingType.FixedDeparture, stay.Type);
        Assert.Equal(departureId, stay.DepartureId);
        Assert.Equal(packageId, stay.PackageId);
        Assert.Null(stay.CustomTripId);
        Assert.Equal(new DateOnly(2026, 12, 22), stay.EndDate);
    }

    [Fact]
    public void ForDeparture_ADayTripWithNoNights_IsAllowed() =>
        Assert.Equal(0, BookingStay.ForDeparture(Guid.NewGuid(), Guid.NewGuid(), Dec20, Dec20, nights: 0).Nights);

    [Fact]
    public void Flexible_WorksOutTheCheckOutDay_AndHasNoDeparture()
    {
        var stay = BookingStay.Flexible(Guid.NewGuid(), Dec20, nights: 2);

        Assert.Equal(BookingType.FlexibleStay, stay.Type);
        Assert.Null(stay.DepartureId);
        Assert.Null(stay.CustomTripId);
        Assert.Equal(new DateOnly(2026, 12, 22), stay.EndDate); // in on the 20th, 2 nights, out on the 22nd
    }

    [Fact]
    public void Flexible_WithNoNights_IsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BookingStay.Flexible(Guid.NewGuid(), Dec20, nights: 0));

    [Fact]
    public void ForCustomTrip_HasNoPackageOrDeparture()
    {
        var tripId = Guid.NewGuid();

        var stay = BookingStay.ForCustomTrip(tripId, Dec20, Dec20.AddDays(5), nights: 5);

        Assert.Equal(BookingType.CustomTrip, stay.Type);
        Assert.Equal(tripId, stay.CustomTripId);
        Assert.Null(stay.PackageId);
        Assert.Null(stay.DepartureId);
    }

    [Fact]
    public void EndingBeforeItStarts_IsRejected() =>
        Assert.Throws<ArgumentException>(() =>
            BookingStay.ForDeparture(Guid.NewGuid(), Guid.NewGuid(), Dec20, Dec20.AddDays(-1), nights: 0));

    [Fact]
    public void AnEmptyId_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => BookingStay.ForDeparture(Guid.Empty, Guid.NewGuid(), Dec20, Dec20, 0));
        Assert.Throws<ArgumentException>(() => BookingStay.Flexible(Guid.Empty, Dec20, 1));
        Assert.Throws<ArgumentException>(() => BookingStay.ForCustomTrip(Guid.Empty, Dec20, Dec20, 0));
    }
}
