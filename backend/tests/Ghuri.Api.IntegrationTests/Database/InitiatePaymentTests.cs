using System.Net;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Payments.Commands.InitiatePayment;
using Ghuri.Domain.Enums;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// "Pay now" against real SQL Server, with a fake SSLCommerz (17-day plan,
/// Day 9: InitiatePayment). Money: the amount sent must be the booking's,
/// and every attempt - even a refused one - must leave a record.
/// </summary>
public class InitiatePaymentTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _sql;

    public InitiatePaymentTests(SqlServerFixture sql)
    {
        _sql = sql;
        _sql.PaymentGateway.Reset(); // each test starts with a gateway that accepts, and no recorded requests
    }

    /// <summary>A fresh pending booking (2 adults, a child, an infant = ৳34,000) and its customer.</summary>
    private async Task<(Guid Customer, string BookingNo)> PendingBookingAsync()
    {
        var (slug, departureId) = await FixedPackageAsync(_sql);
        var customer = await _sql.NewCustomerAsync();
        var booking = await _sql.SendCommandAsync(Book(slug, departureId), customer);
        Assert.True(booking.IsSuccess, booking.Error.Message);
        return (customer, booking.Value.BookingNo);
    }

    private async Task<List<PaymentEntity>> PaymentsForAsync(string bookingNo)
    {
        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await (from p in db.Payments.AsNoTracking()
                      join b in db.Bookings on p.BookingId equals b.Id
                      where b.BookingNo == bookingNo
                      orderby p.PaymentNo
                      select p).ToListAsync();
    }

    [Fact]
    public async Task PayingAPendingBooking_ReturnsThePaymentPage_AndSavesAPendingPayment()
    {
        var (customer, bookingNo) = await PendingBookingAsync();

        var result = await _sql.SendCommandAsync(new InitiatePaymentCommand(bookingNo), customer);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.StartsWith("PAY", result.Value.PaymentNo);
        Assert.Equal($"https://sandbox.example/pay/{result.Value.PaymentNo}", result.Value.PaymentPageUrl);

        var payment = Assert.Single(await PaymentsForAsync(bookingNo));
        Assert.Equal((PaymentStatus.Pending, 34_000m, $"session-{payment.PaymentNo}"),
            (payment.Status, payment.Amount, payment.ProviderSessionId));

        // What the gateway was asked for: the booking's amount, our numbers, the contact's email.
        var sent = Assert.Single(_sql.PaymentGateway.Requests);
        Assert.Equal((payment.PaymentNo, bookingNo, 34_000m, "BDT", "rahim@example.com"),
            (sent.TransactionId, sent.Reference, sent.Amount, sent.Currency, sent.CustomerEmail));
        Assert.Contains(bookingNo, sent.ProductName);
    }

    [Fact]
    public async Task TheGatewayRefuses_TheCustomerGetsAPlainMessage_AndAFailedPaymentIsKept()
    {
        var (customer, bookingNo) = await PendingBookingAsync();
        _sql.PaymentGateway.Respond = _ => PaymentSessionResult.Failure("Store Credential Error Or Store is De-active");

        var result = await _sql.SendCommandAsync(new InitiatePaymentCommand(bookingNo), customer);

        Assert.Equal("payment_start_failed", result.Error.Code);
        var payment = Assert.Single(await PaymentsForAsync(bookingNo)); // kept, despite the failure
        Assert.Equal((PaymentStatus.Failed, "Store Credential Error Or Store is De-active"), (payment.Status, payment.FailureReason));
    }

    [Fact]
    public async Task EveryClick_IsANewAttempt_WithItsOwnNumber()
    {
        var (customer, bookingNo) = await PendingBookingAsync();

        var first = await _sql.SendCommandAsync(new InitiatePaymentCommand(bookingNo), customer);
        var second = await _sql.SendCommandAsync(new InitiatePaymentCommand(bookingNo), customer);

        Assert.NotEqual(first.Value.PaymentNo, second.Value.PaymentNo);
        Assert.Equal(2, (await PaymentsForAsync(bookingNo)).Count);
    }

    [Fact]
    public async Task SomeoneElsesBooking_IsNotFound_AndTheGatewayIsNeverAsked()
    {
        var (_, bookingNo) = await PendingBookingAsync();

        var result = await _sql.SendCommandAsync(new InitiatePaymentCommand(bookingNo), await _sql.NewCustomerAsync());

        Assert.Equal("booking_not_found", result.Error.Code);
        Assert.Empty(await PaymentsForAsync(bookingNo));
        Assert.Empty(_sql.PaymentGateway.Requests);
    }

    [Fact]
    public async Task AfterTheHoldEnded_IsRefused_EvenBeforeTheJobExpiredIt()
    {
        var (customer, bookingNo) = await PendingBookingAsync();
        await EndTheHoldAsync(_sql, bookingNo); // still PendingPayment - the expiry job hasn't run

        var result = await _sql.SendCommandAsync(new InitiatePaymentCommand(bookingNo), customer);

        Assert.Equal("booking_hold_ended", result.Error.Code);
        Assert.Empty(await PaymentsForAsync(bookingNo));
    }

    [Fact]
    public async Task Http_TheGatewaysReturn_IsRecordedOnce_AndSendsTheBrowserToTheResultPage()
    {
        var (customer, bookingNo) = await PendingBookingAsync();
        var paymentNo = (await _sql.SendCommandAsync(new InitiatePaymentCommand(bookingNo), customer)).Value.PaymentNo;
        var client = _sql.CreateClientWithoutRedirects(); // no login: it's SSLCommerz's page that posts here
        var form = new Dictionary<string, string> { ["tran_id"] = paymentNo, ["val_id"] = "2410021530abcdef", ["status"] = "VALID", ["amount"] = "34000.00" };

        // The same form twice - a refresh or the back button.
        HttpResponseMessage? response = null;
        for (var i = 0; i < 2; i++)
            response = await client.PostAsync("/api/v1/payments/sslcommerz/success", new FormUrlEncodedContent(form), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.SeeOther, response!.StatusCode);
        Assert.Equal($"/payment/result?payment={paymentNo}&outcome=success", response.Headers.Location?.OriginalString);

        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var paymentId = await db.Payments.Where(p => p.PaymentNo == paymentNo).Select(p => p.Id).SingleAsync(TestContext.Current.CancellationToken);
        var saved = await db.PaymentEvents.Where(e => e.PaymentId == paymentId).ToListAsync(TestContext.Current.CancellationToken);
        var only = Assert.Single(saved); // recorded once
        Assert.Equal(PaymentEventType.SuccessReturn, only.EventType);
        Assert.Contains("2410021530abcdef", only.PayloadJson);

        // The posted "VALID" counts for nothing: the (fake) validation API
        // refuses this val_id, so the payment is NOT confirmed.
        // PaymentConfirmationTests covers what a validated payment does.
        Assert.Equal(PaymentStatus.Pending, (await PaymentsForAsync(bookingNo)).Single().Status);
        Assert.StartsWith("rejected:", only.ProcessingResult);
    }
}
