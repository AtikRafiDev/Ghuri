using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Booking.Commands.CancelMyBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBookingDocument;
using Ghuri.Application.Features.Payments.Commands.HandleGatewayCallback;
using Ghuri.Application.Features.Payments.Commands.InitiatePayment;
using Ghuri.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Downloading the invoice and the e-voucher (17-day plan, Day 11), against
/// real SQL Server. A voucher only for a confirmed booking; an invoice once
/// money came in; never someone else's.
/// </summary>
public class BookingDocumentTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _sql;

    public BookingDocumentTests(SqlServerFixture sql)
    {
        _sql = sql;
        _sql.PaymentGateway.Reset();
    }

    private async Task<(Guid Customer, string BookingNo)> BookAsync(bool pay)
    {
        var (slug, departureId) = await FixedPackageAsync(_sql);
        var customer = await _sql.NewCustomerAsync();
        var booking = (await _sql.SendCommandAsync(Book(slug, departureId), customer)).Value;
        if (pay)
        {
            var paymentNo = (await _sql.SendCommandAsync(new InitiatePaymentCommand(booking.BookingNo), customer)).Value.PaymentNo;
            _sql.PaymentGateway.ValidatesAs(paymentNo, booking.TotalAmount);
            var paid = await _sql.SendCommandAsync(new HandleGatewayCallbackCommand(
                PaymentEventType.Ipn, new Dictionary<string, string> { ["tran_id"] = paymentNo, ["val_id"] = "v-" + paymentNo }));
            Assert.True(paid.IsSuccess, paid.Error.Message);
        }
        return (customer, booking.BookingNo);
    }

    private static void AssertIsPdf(byte[] content) => Assert.Equal("%PDF", Encoding.ASCII.GetString(content, 0, 4));

    [Theory]
    [InlineData(BookingDocumentKind.Voucher, "Voucher")]
    [InlineData(BookingDocumentKind.Invoice, "Invoice")]
    public async Task AConfirmedBooking_HasBothDocuments(BookingDocumentKind kind, string name)
    {
        var (customer, bookingNo) = await BookAsync(pay: true);

        var file = await _sql.SendAsync(new GetMyBookingDocumentQuery(bookingNo, kind), customer);

        Assert.True(file.IsSuccess, file.Error.Message);
        Assert.Equal($"{name}-{bookingNo}.pdf", file.Value.FileName);
        AssertIsPdf(file.Value.Content);
    }

    [Theory]
    [InlineData(BookingDocumentKind.Voucher)]
    [InlineData(BookingDocumentKind.Invoice)]
    public async Task AnUnpaidBooking_HasNoDocumentsYet(BookingDocumentKind kind)
    {
        var (customer, bookingNo) = await BookAsync(pay: false);

        var file = await _sql.SendAsync(new GetMyBookingDocumentQuery(bookingNo, kind), customer);

        Assert.Equal("document_not_available", file.Error.Code);
    }

    [Fact]
    public async Task AfterCancelling_TheInvoiceStays_ButTheVoucherIsGone()
    {
        var (customer, bookingNo) = await BookAsync(pay: true);
        Assert.True((await _sql.SendCommandAsync(new CancelMyBookingCommand(bookingNo, null), customer)).IsSuccess);

        var invoice = await _sql.SendAsync(new GetMyBookingDocumentQuery(bookingNo, BookingDocumentKind.Invoice), customer);
        var voucher = await _sql.SendAsync(new GetMyBookingDocumentQuery(bookingNo, BookingDocumentKind.Voucher), customer);

        Assert.True(invoice.IsSuccess, invoice.Error.Message); // the receipt for the money paid
        Assert.Equal("document_not_available", voucher.Error.Code); // a voucher would promise a trip that's off
    }

    [Fact]
    public async Task SomeoneElsesDocuments_AreNotFound()
    {
        var (_, bookingNo) = await BookAsync(pay: true);

        var file = await _sql.SendAsync(new GetMyBookingDocumentQuery(bookingNo, BookingDocumentKind.Voucher), await _sql.NewCustomerAsync());

        Assert.Equal("booking_not_found", file.Error.Code);
    }

    [Fact]
    public async Task Http_TheVoucher_DownloadsAsAPdfFile()
    {
        var (customer, bookingNo) = await BookAsync(pay: true);
        var client = _sql.CreateClient();
        await using (var scope = _sql.Services.CreateAsyncScope())
        {
            var user = await scope.ServiceProvider.GetRequiredService<Domain.Repositories.IUserRepository>()
                .GetByIdAsync(customer, TestContext.Current.CancellationToken);
            var token = scope.ServiceProvider.GetRequiredService<ITokenService>().CreateAccessToken(user!);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        }

        var response = await client.GetAsync($"/api/v1/bookings/{bookingNo}/voucher", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal($"Voucher-{bookingNo}.pdf", response.Content.Headers.ContentDisposition?.FileName);
        AssertIsPdf(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }
}
