using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Domain.Tests.Entities.Booking;

public class BookingTests
{
    private static readonly DateOnly Dec20 = new(2026, 12, 20);
    private static readonly DateTime Now = new(2026, 10, 2, 6, 0, 0, DateTimeKind.Utc);

    private static BookingEntity NewBooking(BookingStay stay) =>
        BookingEntity.Create(
            "TB100001", Guid.NewGuid(), stay,
            adults: 2, children: 0, infants: 0,
            adultPriceSnapshot: 10_000, childPriceSnapshot: 0, infantPriceSnapshot: 0,
            subTotal: 20_000, addOnTotal: 0, discountAmount: 0, currency: "BDT",
            PaymentPlan.Full, "Rahim Uddin", PhoneNumber.Create("01700000000"),
            BookingSource.Web, Now, holdExpiresAtUtc: Now.AddMinutes(20));

    [Fact]
    public void Create_Flexible_CopiesTheStay_AndStartsPendingPayment()
    {
        var packageId = Guid.NewGuid();

        var booking = NewBooking(BookingStay.Flexible(packageId, Dec20, nights: 3));

        Assert.Equal(BookingType.FlexibleStay, booking.BookingType);
        Assert.Equal(packageId, booking.PackageId);
        Assert.Null(booking.DepartureId);
        Assert.Equal(Dec20, booking.StartDate);
        Assert.Equal(new DateOnly(2026, 12, 23), booking.EndDate);
        Assert.Equal(3, booking.Nights);
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
        Assert.Equal(20_000, booking.TotalAmount);
    }

    [Fact]
    public void Create_Fixed_LinksTheDeparture_AndRecordsTheFirstStatus()
    {
        var departureId = Guid.NewGuid();

        var booking = NewBooking(BookingStay.ForDeparture(departureId, Guid.NewGuid(), Dec20, Dec20.AddDays(2), nights: 2));

        Assert.Equal(BookingType.FixedDeparture, booking.BookingType);
        Assert.Equal(departureId, booking.DepartureId);
        var first = Assert.Single(booking.History);
        Assert.Null(first.FromStatus);
        Assert.Equal(BookingStatus.PendingPayment, first.ToStatus);
    }
}
