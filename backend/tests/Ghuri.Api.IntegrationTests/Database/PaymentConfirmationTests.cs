using System.Net;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Payments.Commands.HandleGatewayCallback;
using Ghuri.Application.Features.Payments.Commands.InitiatePayment;
using Ghuri.Application.Features.Payments.Queries.GetPaymentResult;
using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Ghuri.Infrastructure.Jobs;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// SSLCommerz's messages against real SQL Server, with a fake SSLCommerz
/// (17-day plan, Day 10: "duplicate IPN → one confirmation; wrong amount →
/// rejected" - mandatory, never skipped). Money: a booking is confirmed only
/// by a validated payment of the right amount, exactly once, and money that
/// arrives too late is never silently lost.
/// </summary>
public class PaymentConfirmationTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _sql;

    public PaymentConfirmationTests(SqlServerFixture sql)
    {
        _sql = sql;
        _sql.PaymentGateway.Reset(); // pages accepted, every val_id refused, nothing recorded
    }

    private sealed record Paying(Guid Customer, string BookingNo, string PaymentNo, decimal Amount, Guid? DepartureId);

    /// <summary>A fresh fixed booking (3 seats, ৳34,000) with a payment started on it - the customer is on SSLCommerz's page.</summary>
    private async Task<Paying> PayingFixedAsync(short seats = 10)
    {
        var (slug, departureId) = await FixedPackageAsync(_sql, seats);
        return await StartPayingAsync(Book(slug, departureId), departureId);
    }

    private async Task<Paying> StartPayingAsync(Application.Features.Booking.Commands.CreateBooking.CreateBookingCommand book, Guid? departureId)
    {
        var customer = await _sql.NewCustomerAsync();
        var booking = await _sql.SendCommandAsync(book, customer);
        Assert.True(booking.IsSuccess, booking.Error.Message);
        var payment = await _sql.SendCommandAsync(new InitiatePaymentCommand(booking.Value.BookingNo), customer);
        Assert.True(payment.IsSuccess, payment.Error.Message);
        return new Paying(customer, booking.Value.BookingNo, payment.Value.PaymentNo, booking.Value.TotalAmount, departureId);
    }

    /// <summary>What SSLCommerz posts: our tran_id, its val_id, and some fields we never trust (status, amount).</summary>
    private static HandleGatewayCallbackCommand Message(PaymentEventType type, string paymentNo, string? valId = "val-1") =>
        new(type, new Dictionary<string, string>
        {
            ["tran_id"] = paymentNo,
            ["val_id"] = valId ?? string.Empty,
            ["status"] = "VALID",
            ["amount"] = "1.00" // a lie on purpose: only the validation API's amount counts
        });

    private async Task<(PaymentEntity Payment, Domain.Entities.Booking.Booking Booking, List<PaymentEvent> Events, int Confirmations)> StateAsync(Paying paying)
    {
        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = await db.Payments.AsNoTracking().SingleAsync(p => p.PaymentNo == paying.PaymentNo);
        var booking = await db.Bookings.AsNoTracking().SingleAsync(b => b.BookingNo == paying.BookingNo);
        var events = await db.PaymentEvents.AsNoTracking().Where(e => e.PaymentId == payment.Id).OrderBy(e => e.Id).ToListAsync();
        var confirmations = await db.BookingStatusHistory.AsNoTracking()
            .CountAsync(h => h.BookingId == booking.Id && h.ToStatus == BookingStatus.Confirmed);
        return (payment, booking, events, confirmations);
    }

    private async Task SucceedAsync(HandleGatewayCallbackCommand message)
    {
        var result = await _sql.SendCommandAsync(message);
        Assert.True(result.IsSuccess, result.Error.Message);
    }

    // ---------- The happy path ----------

    [Fact]
    public async Task AValidatedSuccess_ConfirmsTheBooking_AndRecordsHowItWasPaid()
    {
        var paying = await PayingFixedAsync();
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);

        await SucceedAsync(Message(PaymentEventType.SuccessReturn, paying.PaymentNo));

        var (payment, booking, events, confirmations) = await StateAsync(paying);
        Assert.Equal((PaymentStatus.Succeeded, "BKASH-BKash", $"BANK-{paying.PaymentNo}-val-1"), (payment.Status, payment.Method, payment.ProviderTransactionId));
        Assert.NotNull(payment.PaidAtUtc);
        Assert.Equal((BookingStatus.Confirmed, 34_000m, (DateTime?)null), (booking.Status, booking.PaidAmount, booking.HoldExpiresAtUtc));
        Assert.Equal(1, confirmations);
        Assert.Equal("confirmed", Assert.Single(events).ProcessingResult);
        Assert.Equal(new[] { "val-1" }, _sql.PaymentGateway.Validations); // SSLCommerz was asked
    }

    [Fact]
    public async Task AFlexibleStay_IsConfirmedByTheIpn_Too()
    {
        var slug = await FlexiblePackageAsync(_sql);
        var paying = await StartPayingAsync(Book(slug, startDate: Today.AddDays(10), nights: 4), departureId: null);
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);

        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo));

        var (payment, booking, _, _) = await StateAsync(paying);
        Assert.Equal((PaymentStatus.Succeeded, BookingStatus.Confirmed, paying.Amount), (payment.Status, booking.Status, booking.PaidAmount));
    }

    // ---------- Exactly once ----------

    [Fact]
    public async Task ADuplicateIpn_ConfirmsOnce_AndIsSavedOnce()
    {
        var paying = await PayingFixedAsync();
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);

        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo));
        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo)); // SSLCommerz resends

        var (_, booking, events, confirmations) = await StateAsync(paying);
        Assert.Equal((34_000m, 1), (booking.PaidAmount, confirmations)); // paid once, confirmed once
        Assert.Single(events);
        Assert.Single(_sql.PaymentGateway.Validations); // the repeat was skipped before asking SSLCommerz again
    }

    [Fact]
    public async Task TheIpnAndTheBrowser_ArrivingTogether_ConfirmOnce()
    {
        // Run it several times: a race that loses only now and then is still a bug.
        for (var round = 0; round < 5; round++)
        {
            var paying = await PayingFixedAsync();
            _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);

            var results = await Task.WhenAll(
                _sql.SendCommandAsync(Message(PaymentEventType.Ipn, paying.PaymentNo)),
                _sql.SendCommandAsync(Message(PaymentEventType.SuccessReturn, paying.PaymentNo)));

            Assert.All(results, r => Assert.True(r.IsSuccess, r.Error.Message));
            var (payment, booking, events, confirmations) = await StateAsync(paying);
            Assert.Equal(PaymentStatus.Succeeded, payment.Status);
            Assert.Equal((34_000m, 1), (booking.PaidAmount, confirmations)); // never ৳68,000
            Assert.Equal(new[] { "already confirmed", "confirmed" }, events.Select(e => e.ProcessingResult!).Order());
        }
    }

    // ---------- Rejected ----------

    [Fact]
    public async Task AWrongAmount_IsRejected_AndTheBookingStaysUnpaid()
    {
        var paying = await PayingFixedAsync();
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, 100m); // real payment, but ৳100 instead of ৳34,000

        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo));

        var (payment, booking, events, _) = await StateAsync(paying);
        Assert.Equal((PaymentStatus.Pending, BookingStatus.PendingPayment, 0m), (payment.Status, booking.Status, booking.PaidAmount));
        Assert.Equal("rejected: amount 100.00 BDT, expected 34000.00 BDT", Assert.Single(events).ProcessingResult);
    }

    [Fact]
    public async Task AWrongCurrency_IsRejected()
    {
        var paying = await PayingFixedAsync();
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount, currency: "USD");

        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo));

        Assert.Equal(BookingStatus.PendingPayment, (await StateAsync(paying)).Booking.Status);
    }

    [Fact]
    public async Task AValIdFromAnotherPayment_IsRejected()
    {
        var paying = await PayingFixedAsync();
        // A real val_id - but SSLCommerz says it belongs to some other transaction.
        _sql.PaymentGateway.ValidatesAs("PAY999999", paying.Amount);

        await SucceedAsync(Message(PaymentEventType.SuccessReturn, paying.PaymentNo));

        var (payment, booking, events, _) = await StateAsync(paying);
        Assert.Equal((PaymentStatus.Pending, BookingStatus.PendingPayment), (payment.Status, booking.Status));
        Assert.StartsWith("rejected: the validation is for transaction PAY999999", Assert.Single(events).ProcessingResult);
    }

    [Fact]
    public async Task AFakeValId_IsRejected()
    {
        var paying = await PayingFixedAsync(); // the fake gateway refuses every val_id by default

        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo, valId: "made-up"));

        var (payment, booking, events, _) = await StateAsync(paying);
        Assert.Equal((PaymentStatus.Pending, BookingStatus.PendingPayment), (payment.Status, booking.Status));
        Assert.StartsWith("rejected:", Assert.Single(events).ProcessingResult);
    }

    [Fact]
    public async Task FailAndCancelMessages_AreOnlyRecorded()
    {
        var paying = await PayingFixedAsync();
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);

        await SucceedAsync(Message(PaymentEventType.FailReturn, paying.PaymentNo));
        await SucceedAsync(Message(PaymentEventType.CancelReturn, paying.PaymentNo));

        var (payment, booking, events, _) = await StateAsync(paying);
        Assert.Equal((PaymentStatus.Pending, BookingStatus.PendingPayment), (payment.Status, booking.Status)); // anyone can post a "fail"
        Assert.All(events, e => Assert.Equal("recorded", e.ProcessingResult));
        Assert.Empty(_sql.PaymentGateway.Validations);
    }

    [Fact]
    public async Task SslCommerzNotAnswering_SavesNothing_SoTheRetryWorks()
    {
        var paying = await PayingFixedAsync();
        _sql.PaymentGateway.Validate = _ => PaymentValidationResult.Unreachable("timeout");

        var first = await _sql.SendCommandAsync(Message(PaymentEventType.Ipn, paying.PaymentNo));

        Assert.Equal("payment_gateway_unreachable", first.Error.Code);
        Assert.Empty((await StateAsync(paying)).Events); // not marked as handled...

        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);
        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo)); // ...so SSLCommerz's resend is handled properly

        Assert.Equal(BookingStatus.Confirmed, (await StateAsync(paying)).Booking.Status);
    }

    // ---------- Late or extra money ----------

    private async Task ExpireAsync(Paying paying)
    {
        await EndTheHoldAsync(_sql, paying.BookingNo);
        await _sql.Services.GetRequiredService<BookingExpiryJob>().RunOnceAsync(TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Expired, (await StateAsync(paying)).Booking.Status);
    }

    [Fact]
    public async Task PaidAfterExpiry_WithTheSeatsStillFree_RevivesTheBooking()
    {
        var paying = await PayingFixedAsync(seats: 10);
        await ExpireAsync(paying);
        Assert.Equal(0, await _sql.ReservedSeatsAsync(paying.DepartureId!.Value));
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);

        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo));

        var (_, booking, events, _) = await StateAsync(paying);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(3, await _sql.ReservedSeatsAsync(paying.DepartureId.Value)); // seats taken again
        Assert.Equal("confirmed after expiry", Assert.Single(events).ProcessingResult);
    }

    [Fact]
    public async Task PaidAfterExpiry_WithTheSeatsGone_KeepsTheMoneyOnTheBooking_ForARefund()
    {
        var paying = await PayingFixedAsync(seats: 3);
        await ExpireAsync(paying);
        // Someone else takes all 3 seats meanwhile.
        await using (var scope = _sql.Services.CreateAsyncScope())
        {
            var departures = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IDepartureRepository>();
            Assert.True(await departures.TryReserveSeatsAsync(paying.DepartureId!.Value, 3, TestContext.Current.CancellationToken));
        }
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);

        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo));

        var (payment, booking, events, _) = await StateAsync(paying);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status); // the money did arrive...
        Assert.Equal((BookingStatus.Expired, 34_000m), (booking.Status, booking.PaidAmount)); // ...and shows on the booking, which stays expired
        Assert.Equal(3, await _sql.ReservedSeatsAsync(paying.DepartureId.Value)); // nobody's seats were taken twice
        Assert.StartsWith("refund due:", Assert.Single(events).ProcessingResult);
    }

    [Fact]
    public async Task ASecondPaymentForAPaidBooking_IsARefund_NotASecondConfirmation()
    {
        var paying = await PayingFixedAsync();
        // The customer opened the payment page twice and paid on both.
        var second = (await _sql.SendCommandAsync(new InitiatePaymentCommand(paying.BookingNo), paying.Customer)).Value.PaymentNo;
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);
        await SucceedAsync(Message(PaymentEventType.Ipn, paying.PaymentNo, valId: "val-first"));

        _sql.PaymentGateway.ValidatesAs(second, paying.Amount);
        await SucceedAsync(Message(PaymentEventType.Ipn, second, valId: "val-second"));

        var secondState = await StateAsync(paying with { PaymentNo = second });
        Assert.Equal(PaymentStatus.Succeeded, secondState.Payment.Status);
        // The booking counts its price once; the extra ৳34,000 stays on the second Payment, to refund.
        Assert.Equal((BookingStatus.Confirmed, 34_000m, 1), (secondState.Booking.Status, secondState.Booking.PaidAmount, secondState.Confirmations));
        Assert.StartsWith("refund due: paid, but the booking was already paid (Confirmed) (RF", Assert.Single(secondState.Events).ProcessingResult);

        // Day 12: the system asked for the refund itself - it's in the staff's list, for the whole 2nd payment.
        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var refund = await db.Refunds.AsNoTracking().SingleAsync(r => r.PaymentId == secondState.Payment.Id, TestContext.Current.CancellationToken);
        Assert.Equal((paying.Amount, 100m, RefundStatus.Requested, (Guid?)null), (refund.Amount, refund.RefundPercent, refund.Status, refund.RequestedBy));
    }

    // ---------- The result page's question ----------

    [Fact]
    public async Task ThePaymentResult_ShowsTheOwnerWhatHappened_AndNobodyElse()
    {
        var paying = await PayingFixedAsync();

        var before = await _sql.SendAsync(new GetPaymentResultQuery(paying.PaymentNo), paying.Customer);
        Assert.Equal((PaymentStatus.Pending, BookingStatus.PendingPayment), (before.Value.Status, before.Value.BookingStatus));

        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);
        await SucceedAsync(Message(PaymentEventType.SuccessReturn, paying.PaymentNo));

        var after = await _sql.SendAsync(new GetPaymentResultQuery(paying.PaymentNo), paying.Customer);
        Assert.Equal((PaymentStatus.Succeeded, BookingStatus.Confirmed, paying.BookingNo), (after.Value.Status, after.Value.BookingStatus, after.Value.BookingNo));

        var stranger = await _sql.SendAsync(new GetPaymentResultQuery(paying.PaymentNo), await _sql.NewCustomerAsync());
        Assert.Equal("payment_not_found", stranger.Error.Code);
    }

    // ---------- Over HTTP ----------

    [Fact]
    public async Task Http_TheIpn_Answers200WhenHandled_And503WhenSslCommerzCantBeAsked()
    {
        var paying = await PayingFixedAsync();
        var client = _sql.CreateClientWithoutRedirects(); // no login: it's SSLCommerz's server
        var form = new Dictionary<string, string> { ["tran_id"] = paying.PaymentNo, ["val_id"] = "val-http", ["status"] = "VALID" };

        _sql.PaymentGateway.Validate = _ => PaymentValidationResult.Unreachable("timeout");
        var unreachable = await client.PostAsync("/api/v1/payments/sslcommerz/ipn", new FormUrlEncodedContent(form), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unreachable.StatusCode); // SSLCommerz will resend

        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);
        var handled = await client.PostAsync("/api/v1/payments/sslcommerz/ipn", new FormUrlEncodedContent(form), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, handled.StatusCode);
        Assert.Equal(BookingStatus.Confirmed, (await StateAsync(paying)).Booking.Status);
    }

    [Fact]
    public async Task Http_TheBrowsersReturn_ConfirmsThePayment_AndGoesToTheResultPage()
    {
        var paying = await PayingFixedAsync();
        _sql.PaymentGateway.ValidatesAs(paying.PaymentNo, paying.Amount);
        var client = _sql.CreateClientWithoutRedirects();
        var form = new Dictionary<string, string> { ["tran_id"] = paying.PaymentNo, ["val_id"] = "val-browser" };

        var response = await client.PostAsync("/api/v1/payments/sslcommerz/success", new FormUrlEncodedContent(form), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal($"/payment/result?payment={paying.PaymentNo}&outcome=success", response.Headers.Location?.OriginalString);
        Assert.Equal(BookingStatus.Confirmed, (await StateAsync(paying)).Booking.Status); // works locally, where no IPN can arrive
    }
}
