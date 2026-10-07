using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.ValueObjects;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Domain.Tests.Entities.Payment;

/// <summary>
/// Payment's start and its state machine (17-day plan, Days 9-10). Money: the
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
                TourType.Group, [], [], null, null, false),
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

    // ---------- Success (Day 10) ----------

    [Fact]
    public void MarkSucceeded_KeepsHowAndWhenItWasPaid()
    {
        var payment = NewPayment();
        payment.MarkSessionCreated("abc");

        payment.MarkSucceeded("BKASH-BKash", "2410021530ABC", 240m, Now.AddMinutes(8));

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(("BKASH-BKash", "2410021530ABC", (decimal?)240m, (DateTime?)Now.AddMinutes(8)),
            (payment.Method, payment.ProviderTransactionId, payment.GatewayFee, payment.PaidAtUtc));
    }

    [Fact]
    public void AValidatedSuccess_AfterAFailMessage_StillCounts()
    {
        var payment = NewPayment();
        payment.MarkFailed("Customer closed the page"); // messages arrive out of order

        payment.MarkSucceeded("VISA-Dutch Bangla", "BANK1", null, Now.AddMinutes(9));

        Assert.Equal((PaymentStatus.Succeeded, (string?)null), (payment.Status, payment.FailureReason));
    }

    [Fact]
    public void MarkSucceeded_Twice_IsRefused_SoMoneyIsNeverCountedTwice()
    {
        var payment = NewPayment();
        payment.MarkSucceeded("BKASH-BKash", "BANK1", null, Now);

        Assert.Equal("payment_already_settled",
            Assert.Throws<DomainException>(() => payment.MarkSucceeded("BKASH-BKash", "BANK1", null, Now)).Code);
    }

    [Fact]
    public void AFailMessage_AfterSuccess_ChangesNothing()
    {
        var payment = NewPayment();
        payment.MarkSucceeded("BKASH-BKash", "BANK1", null, Now);

        payment.MarkFailed("A late fail");

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
    }

    [Fact]
    public void MarkSucceeded_CutsLongGatewayTexts_ToTheirColumns()
    {
        var payment = NewPayment();

        payment.MarkSucceeded(new string('m', 50), new string('t', 150), -5m, Now);

        Assert.Equal((30, 100, (decimal?)null), (payment.Method!.Length, payment.ProviderTransactionId!.Length, payment.GatewayFee));
    }

    // ---------- Refunded (Day 12) ----------

    [Theory]
    [InlineData(16_000, PaymentStatus.Refunded)]
    [InlineData(8_000, PaymentStatus.PartiallyRefunded)]
    public void MarkRefunded_AllOrPartOfIt(decimal totalRefunded, PaymentStatus expected)
    {
        var payment = NewPayment(); // ৳16,000
        payment.MarkSucceeded("BKASH-BKash", "BANK1", null, Now);

        payment.MarkRefunded(totalRefunded);

        Assert.Equal(expected, payment.Status);
    }

    [Fact]
    public void APaymentThatNeverSucceeded_CantBeRefunded()
    {
        var payment = NewPayment();
        payment.MarkFailed("Declined");

        Assert.Equal("payment_not_refundable", Assert.Throws<DomainException>(() => payment.MarkRefunded(100)).Code);
    }

    [Fact]
    public void AVeryLongGatewayReason_IsCutTo300Characters()
    {
        var payment = NewPayment();

        payment.MarkFailed(new string('x', 500));

        Assert.Equal(300, payment.FailureReason!.Length);
    }
}
