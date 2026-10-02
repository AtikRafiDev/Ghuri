using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.ValueObjects;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Domain.Tests.Entities.Payment;

/// <summary>
/// Payment's start and its state machine (17-day plan, Day 9). Money: the
/// amount must always be the booking's, and a success must never be undone
/// by a late or repeated failure message.
/// </summary>
public class PaymentTests
{
    private static readonly DateOnly Today = new(2026, 12, 1);
    private static readonly DateTime Now = new(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc);

    /// <summary>A pending booking of a flexible stay: 2 adults × ৳8,000 (2 nights) = ৳16,000, held until Now + 20 minutes.</summary>
    private static BookingEntity PendingBooking()
    {
        var package = TourPackage.Create(
            "PKG1001",
            new TourPackageDetails(Guid.NewGuid(), "Beach Escape", Slug.Create("Beach Escape"), "Summary", null,
                TourType.Group, [], [], null, null, false, null, null),
            PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3));
        package.SetImages([Guid.NewGuid()]);
        package.SetItinerary([new ItineraryDayDetails("Day 1", "Plan"), new ItineraryDayDetails("Day 2", "Plan")]);
        package.Publish(Now, hasOpenDeparture: true);

        return BookingEntity.CreateFlexible(
            "TB100001", Guid.NewGuid(), package, new DateOnly(2026, 12, 20), 2,
            [new TravellerDetails(TravellerType.Adult, "Rahim Uddin", true), new TravellerDetails(TravellerType.Adult, "Karima Begum", false)],
            new BookingContact("Rahim Uddin", PhoneNumber.Create("01700000000"), "rahim@example.com"),
            null, BookingSource.Web, Today, Now);
    }

    private static PaymentEntity NewPayment(BookingEntity? booking = null) =>
        PaymentEntity.StartFor(booking ?? PendingBooking(), "PAY100001", PaymentProvider.SslCommerz, Now.AddMinutes(2));

    // ---------- Starting ----------

    [Fact]
    public void StartFor_TakesTheAmountAndCurrencyFromTheBooking()
    {
        var booking = PendingBooking();

        var payment = NewPayment(booking);

        Assert.Equal((booking.Id, 16_000m, "BDT"), (payment.BookingId, payment.Amount, payment.Currency));
        Assert.Equal(PaymentStatus.Initiated, payment.Status);
        Assert.Equal("PAY100001", payment.PaymentNo);
    }

    [Fact]
    public void StartFor_AfterTheHoldEnded_IsRefused_EvenBeforeTheJobExpiredIt()
    {
        var booking = PendingBooking(); // still PendingPayment - the job hasn't run

        var error = Assert.Throws<DomainException>(() =>
            PaymentEntity.StartFor(booking, "PAY100001", PaymentProvider.SslCommerz, Now.AddMinutes(20)));

        Assert.Equal("booking_not_payable", error.Code);
    }

    [Fact]
    public void StartFor_ACancelledBooking_IsRefused()
    {
        var booking = PendingBooking();
        booking.Cancel(Now.AddMinutes(1), "Changed my mind", null);

        Assert.Equal("booking_not_payable", Assert.Throws<DomainException>(() => NewPayment(booking)).Code);
    }

    // ---------- State machine ----------

    [Fact]
    public void MarkSessionCreated_MakesItPending_AndKeepsTheSession()
    {
        var payment = NewPayment();

        payment.MarkSessionCreated("F2A7C1E3B9");

        Assert.Equal((PaymentStatus.Pending, "F2A7C1E3B9"), (payment.Status, payment.ProviderSessionId));
    }

    [Fact]
    public void MarkSessionCreated_Twice_IsRefused()
    {
        var payment = NewPayment();
        payment.MarkSessionCreated("first");

        Assert.Equal("payment_not_initiated", Assert.Throws<DomainException>(() => payment.MarkSessionCreated("second")).Code);
    }

    [Fact]
    public void MarkFailed_EndsAPaymentInProgress_WithTheReason()
    {
        var payment = NewPayment();

        payment.MarkFailed("  Store credentials rejected  ");

        Assert.Equal((PaymentStatus.Failed, "Store credentials rejected"), (payment.Status, payment.FailureReason));
    }

    [Fact]
    public void MarkCancelled_EndsAPendingPayment()
    {
        var payment = NewPayment();
        payment.MarkSessionCreated("abc");

        payment.MarkCancelled("Customer cancelled on the payment page");

        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
    }

    [Fact]
    public void ARepeatedFailMessage_ChangesNothing()
    {
        var payment = NewPayment();
        payment.MarkFailed("First message");

        payment.MarkFailed("The same message, resent");
        payment.MarkCancelled("A cancel arriving late");

        Assert.Equal((PaymentStatus.Failed, "First message"), (payment.Status, payment.FailureReason));
    }

    [Fact]
    public void AVeryLongGatewayReason_IsCutTo300Characters()
    {
        var payment = NewPayment();

        payment.MarkFailed(new string('x', 500));

        Assert.Equal(300, payment.FailureReason!.Length);
    }
}
