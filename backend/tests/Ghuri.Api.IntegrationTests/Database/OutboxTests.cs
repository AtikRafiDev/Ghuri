using Ghuri.Application.Features.Payments.Commands.HandleGatewayCallback;
using Ghuri.Application.Features.Payments.Commands.InitiatePayment;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Enums;
using Ghuri.Infrastructure.Jobs;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// The transactional outbox against real SQL Server (17-day plan, Day 11:
/// "BookingConfirmed event (outbox) → email"). The confirmation email must
/// follow every confirmed booking - even when the mail server is down for a
/// while - and never follow a booking that wasn't confirmed.
/// </summary>
public class OutboxTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _sql;

    public OutboxTests(SqlServerFixture sql)
    {
        _sql = sql;
        _sql.PaymentGateway.Reset();
        _sql.Emails.Reset();
    }

    private OutboxDispatcherJob Dispatcher => _sql.Services.GetRequiredService<OutboxDispatcherJob>();

    /// <summary>Books (3 paying travellers, ৳34,000) and sends SSLCommerz's IPN. validAmount: what the (fake) validation API reports.</summary>
    private async Task<string> BookAndPayAsync(decimal? validAmount = null)
    {
        var (slug, departureId) = await FixedPackageAsync(_sql);
        var customer = await _sql.NewCustomerAsync();
        var booking = (await _sql.SendCommandAsync(Book(slug, departureId), customer)).Value;
        var paymentNo = (await _sql.SendCommandAsync(new InitiatePaymentCommand(booking.BookingNo), customer)).Value.PaymentNo;
        _sql.PaymentGateway.ValidatesAs(paymentNo, validAmount ?? booking.TotalAmount);
        await _sql.SendCommandAsync(new HandleGatewayCallbackCommand(
            PaymentEventType.Ipn, new Dictionary<string, string> { ["tran_id"] = paymentNo, ["val_id"] = "v-" + paymentNo }));
        return booking.BookingNo;
    }

    private async Task<List<Domain.Entities.Ops.OutboxMessage>> OutboxForAsync(string bookingNo)
    {
        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingId = await db.Bookings.Where(b => b.BookingNo == bookingNo).Select(b => b.Id).SingleAsync();
        var id = bookingId.ToString();
        return await db.OutboxMessages.AsNoTracking()
            .Where(m => m.Type == typeof(BookingConfirmed).FullName && m.PayloadJson.Contains(id))
            .ToListAsync();
    }

    private List<Application.Abstractions.Ports.EmailMessage> EmailsAbout(string bookingNo) =>
        _sql.Emails.Sent.Where(e => e.Subject.Contains(bookingNo)).ToList();

    [Fact]
    public async Task AConfirmedBooking_QueuesOneEvent_AndTheJobEmailsTheCustomer()
    {
        var bookingNo = await BookAndPayAsync();

        var queued = Assert.Single(await OutboxForAsync(bookingNo)); // saved WITH the confirmation
        Assert.Null(queued.ProcessedAtUtc);
        Assert.Empty(EmailsAbout(bookingNo)); // nothing is sent inside the payment's transaction

        await Dispatcher.RunOnceAsync(TestContext.Current.CancellationToken);

        var email = Assert.Single(EmailsAbout(bookingNo));
        Assert.Equal("rahim@example.com", email.To);
        Assert.Contains("confirmed", email.HtmlBody);
        Assert.Contains("৳34,000", email.HtmlBody);
        // Day 11: "email with PDFs" - the voucher and the invoice, both real PDFs.
        Assert.Equal(new[] { $"Voucher-{bookingNo}.pdf", $"Invoice-{bookingNo}.pdf" }, email.Attachments!.Select(a => a.FileName));
        Assert.All(email.Attachments!, a => Assert.Equal(("application/pdf", "%PDF"),
            (a.ContentType, System.Text.Encoding.ASCII.GetString(a.Content, 0, 4))));
        Assert.NotNull(Assert.Single(await OutboxForAsync(bookingNo)).ProcessedAtUtc);

        await Dispatcher.RunOnceAsync(TestContext.Current.CancellationToken);
        Assert.Single(EmailsAbout(bookingNo)); // handled once - the next run doesn't send it again
    }

    [Theory]
    [InlineData(0, 0)] // a new event: at once
    [InlineData(1, 10)] // first retry: 10 s after the event
    [InlineData(2, 30)] // then 20 s more
    [InlineData(3, 70)] // then 40 s more
    [InlineData(9, 5_110)] // the 10th and last try: ~1.4 hours after the event
    public void Retries_WaitLongerEachTime(byte attempts, int expectedSeconds) =>
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), OutboxDispatcherJob.RetryWait(attempts, retryDelaySeconds: 10));

    [Fact]
    public async Task ARejectedPayment_QueuesNothing()
    {
        var bookingNo = await BookAndPayAsync(validAmount: 100m); // wrong amount → not confirmed

        Assert.Empty(await OutboxForAsync(bookingNo));
    }

    [Fact]
    public async Task TheMailServerBeingDown_IsRetried_UntilItWorks()
    {
        var bookingNo = await BookAndPayAsync();
        _sql.Emails.FailWith = new InvalidOperationException("SMTP server not reachable");

        await Dispatcher.RunOnceAsync(TestContext.Current.CancellationToken);

        var failed = Assert.Single(await OutboxForAsync(bookingNo));
        Assert.Equal(((DateTime?)null, (byte)1), (failed.ProcessedAtUtc, failed.Attempts));
        Assert.Contains("SMTP server not reachable", failed.Error);

        _sql.Emails.FailWith = null; // back up
        await Dispatcher.RunOnceAsync(TestContext.Current.CancellationToken);

        Assert.Single(EmailsAbout(bookingNo));
        var done = Assert.Single(await OutboxForAsync(bookingNo));
        Assert.Equal(((byte)2, (string?)null), (done.Attempts, done.Error));
        Assert.NotNull(done.ProcessedAtUtc);
    }
}
