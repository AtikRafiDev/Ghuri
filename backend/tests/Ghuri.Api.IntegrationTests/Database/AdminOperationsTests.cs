using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Booking.Commands.CancelBookingByAgency;
using Ghuri.Application.Features.Booking.Queries.GetBookingForAdmin;
using Ghuri.Application.Features.Booking.Queries.SearchBookings;
using Ghuri.Application.Features.Dashboard.Queries.GetAdminDashboard;
using Ghuri.Application.Features.Payments.Commands.CompleteRefund;
using Ghuri.Application.Features.Payments.Commands.HandleGatewayCallback;
using Ghuri.Application.Features.Payments.Commands.InitiatePayment;
using Ghuri.Application.Features.Payments.Commands.RecordManualPayment;
using Ghuri.Application.Features.Payments.Commands.RejectRefund;
using Ghuri.Application.Features.Payments.Queries.SearchPayments;
using Ghuri.Application.Features.Payments.Queries.SearchRefunds;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Jobs;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Staff running the day from the admin panel (17-day plan, Day 12), against
/// real SQL Server. Money: a manual payment confirms once and only for the
/// right amount; an agency cancellation refunds everything; a refund is
/// marked as sent once; and only the right roles can do any of it.
/// </summary>
public class AdminOperationsTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _sql;

    public AdminOperationsTests(SqlServerFixture sql)
    {
        _sql = sql;
        _sql.PaymentGateway.Reset();
    }

    private sealed record Booked(Guid Customer, string BookingNo, decimal Total, Guid DepartureId);

    private async Task<Booked> PendingAsync(short seats = 10)
    {
        var (slug, departureId) = await FixedPackageAsync(_sql, seats);
        var customer = await _sql.NewCustomerAsync();
        var booking = (await _sql.SendCommandAsync(Book(slug, departureId), customer)).Value;
        return new Booked(customer, booking.BookingNo, booking.TotalAmount, departureId);
    }

    private async Task<Booked> PaidOnlineAsync()
    {
        var booked = await PendingAsync();
        var paymentNo = (await _sql.SendCommandAsync(new InitiatePaymentCommand(booked.BookingNo), booked.Customer)).Value.PaymentNo;
        _sql.PaymentGateway.ValidatesAs(paymentNo, booked.Total);
        var paid = await _sql.SendCommandAsync(new HandleGatewayCallbackCommand(
            PaymentEventType.Ipn, new Dictionary<string, string> { ["tran_id"] = paymentNo, ["val_id"] = "v-" + paymentNo }));
        Assert.True(paid.IsSuccess, paid.Error.Message);
        return booked;
    }

    private Task<Guid> StaffAsync() => _sql.NewCustomerAsync(); // outside HTTP the role isn't checked - any real user id will do

    private Task<AdminBookingDto> AdminViewAsync(string bookingNo) =>
        _sql.SendAsync(new GetBookingForAdminQuery(bookingNo)).ContinueWith(t => t.Result.Value);

    // ---------- Manual payment ----------

    [Fact]
    public async Task AManualPayment_ConfirmsTheBooking_LikeAnOnlineOne()
    {
        var booked = await PendingAsync();
        var staff = await StaffAsync();

        var result = await _sql.SendCommandAsync(
            new RecordManualPaymentCommand(booked.BookingNo, booked.Total, ManualPaymentMethod.BankTransfer, "DBBL-778812"), staff);

        Assert.True(result.IsSuccess, result.Error.Message);
        var view = await AdminViewAsync(booked.BookingNo);
        Assert.Equal((BookingStatus.Confirmed, booked.Total, 0m), (view.Status, view.PaidAmount, view.AmountDue));
        var payment = Assert.Single(view.Payments);
        Assert.Equal((PaymentProvider.Manual, "Bank transfer", PaymentStatus.Succeeded, "DBBL-778812"),
            (payment.Provider, payment.Method, payment.Status, payment.Reference));
        Assert.Equal(staff, (await HistoryAsync(booked.BookingNo)).Last().ChangedBy); // who confirmed it is on record

        // ...and the voucher email is queued, exactly like after SSLCommerz (the event carries the booking's id).
        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bookingId = (await db.Bookings.SingleAsync(b => b.BookingNo == booked.BookingNo)).Id.ToString();
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Type == typeof(Domain.Entities.Booking.BookingConfirmed).FullName && m.PayloadJson.Contains(bookingId)));
    }

    [Fact]
    public async Task AManualPayment_OfTheWrongAmount_IsRefused()
    {
        var booked = await PendingAsync();

        var result = await _sql.SendCommandAsync(
            new RecordManualPaymentCommand(booked.BookingNo, booked.Total - 1, ManualPaymentMethod.Cash, null), await StaffAsync());

        Assert.Equal("manual_amount_mismatch", result.Error.Code);
        Assert.Empty((await AdminViewAsync(booked.BookingNo)).Payments);
    }

    [Fact]
    public async Task TheSameTransactionId_CantBeRecordedTwice()
    {
        var first = await PendingAsync();
        var second = await PendingAsync();
        var staff = await StaffAsync();
        await _sql.SendCommandAsync(new RecordManualPaymentCommand(first.BookingNo, first.Total, ManualPaymentMethod.BKash, "BK-9X1"), staff);

        var again = await _sql.SendCommandAsync(new RecordManualPaymentCommand(second.BookingNo, second.Total, ManualPaymentMethod.BKash, "BK-9X1"), staff);

        Assert.Equal("payment_reference_used", again.Error.Code);
    }

    [Fact]
    public async Task APaidBooking_CantBePaidAgain()
    {
        var booked = await PaidOnlineAsync();

        var result = await _sql.SendCommandAsync(
            new RecordManualPaymentCommand(booked.BookingNo, booked.Total, ManualPaymentMethod.Cash, null), await StaffAsync());

        Assert.Equal("booking_not_payable", result.Error.Code);
    }

    [Fact]
    public async Task AnExpiredBooking_PaidAtTheCounter_TakesItsSeatsBack()
    {
        var booked = await PendingAsync(seats: 10);
        await ExpireAsync(booked);

        var result = await _sql.SendCommandAsync(
            new RecordManualPaymentCommand(booked.BookingNo, booked.Total, ManualPaymentMethod.Cash, null), await StaffAsync());

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal(BookingStatus.Confirmed, result.Value.BookingStatus);
        Assert.Equal(3, await _sql.ReservedSeatsAsync(booked.DepartureId));
    }

    [Fact]
    public async Task AnExpiredBooking_WhoseSeatsAreGone_CantBePaid_AndNothingIsRecorded()
    {
        var booked = await PendingAsync(seats: 3);
        await ExpireAsync(booked);
        await using (var scope = _sql.Services.CreateAsyncScope())
        {
            var departures = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IDepartureRepository>();
            Assert.True(await departures.TryReserveSeatsAsync(booked.DepartureId, 3, TestContext.Current.CancellationToken));
        }

        var result = await _sql.SendCommandAsync(
            new RecordManualPaymentCommand(booked.BookingNo, booked.Total, ManualPaymentMethod.Cash, null), await StaffAsync());

        Assert.Equal("seats_no_longer_available", result.Error.Code);
        var view = await AdminViewAsync(booked.BookingNo);
        Assert.Equal((BookingStatus.Expired, 0m), (view.Status, view.PaidAmount));
        Assert.Empty(view.Payments);
        Assert.Equal(3, await _sql.ReservedSeatsAsync(booked.DepartureId)); // still only the other customer's 3
    }

    // ---------- Agency cancellation ----------

    [Fact]
    public async Task TheAgencyCancelling_RefundsEverything_AndFreesTheSeats()
    {
        var booked = await PaidOnlineAsync();
        var staff = await StaffAsync();

        var result = await _sql.SendCommandAsync(new CancelBookingByAgencyCommand(booked.BookingNo, "Hotel fully booked"), staff);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal(booked.Total, result.Value.RefundAmount); // 100%, whatever the policy says
        var view = await AdminViewAsync(booked.BookingNo);
        Assert.Equal((BookingStatus.Cancelled, "Hotel fully booked"), (view.Status, view.CancelReason));
        var refund = Assert.Single(view.Refunds);
        Assert.Equal((booked.Total, 100m, RefundStatus.Requested), (refund.Amount, refund.RefundPercent, refund.Status));
        Assert.NotNull(refund.RequestedByName);
        Assert.Equal(0, await _sql.ReservedSeatsAsync(booked.DepartureId));
    }

    [Fact]
    public async Task TheAgencyCancellingAnUnpaidBooking_RefundsNothing()
    {
        var booked = await PendingAsync();

        var result = await _sql.SendCommandAsync(new CancelBookingByAgencyCommand(booked.BookingNo, "Duplicate booking"), await StaffAsync());

        Assert.Equal((0m, (string?)null), (result.Value.RefundAmount, result.Value.RefundNo));
        Assert.Empty((await AdminViewAsync(booked.BookingNo)).Refunds);
    }

    [Fact]
    public async Task ACancellationWithoutAReason_IsAValidationError()
    {
        var booked = await PendingAsync();

        var result = await _sql.SendCommandAsync(new CancelBookingByAgencyCommand(booked.BookingNo, "  "), await StaffAsync());

        Assert.Equal("validation_failed", result.Error.Code);
    }

    // ---------- Refunds ----------

    [Fact]
    public async Task MarkingARefundSent_CompletesIt_AndRefundsThePayment_OnlyOnce()
    {
        var booked = await PaidOnlineAsync();
        var staff = await StaffAsync();
        var refundNo = (await _sql.SendCommandAsync(new CancelBookingByAgencyCommand(booked.BookingNo, "Departure called off"), staff)).Value.RefundNo!;

        var done = await _sql.SendCommandAsync(new CompleteRefundCommand(refundNo, " BK-REF-551 "), staff);
        var twice = await _sql.SendCommandAsync(new CompleteRefundCommand(refundNo, "BK-REF-552"), staff);

        Assert.True(done.IsSuccess, done.Error.Message);
        Assert.Equal("refund_not_open", twice.Error.Code);
        var view = await AdminViewAsync(booked.BookingNo);
        var refund = Assert.Single(view.Refunds);
        Assert.Equal((RefundStatus.Completed, "BK-REF-551"), (refund.Status, refund.Reference));
        Assert.NotNull(refund.CompletedAtUtc);
        Assert.Equal(PaymentStatus.Refunded, Assert.Single(view.Payments).Status);
    }

    [Fact]
    public async Task APartRefund_LeavesThePaymentPartlyRefunded()
    {
        await EnsureGlobalPolicyAsync();
        var slug = await FlexiblePackageAsync(_sql);
        var customer = await _sql.NewCustomerAsync();
        var booking = (await _sql.SendCommandAsync(Book(slug, startDate: Today.AddDays(20), nights: 4), customer)).Value; // 20 days → 50%
        var paymentNo = (await _sql.SendCommandAsync(new InitiatePaymentCommand(booking.BookingNo), customer)).Value.PaymentNo;
        _sql.PaymentGateway.ValidatesAs(paymentNo, booking.TotalAmount);
        await _sql.SendCommandAsync(new HandleGatewayCallbackCommand(
            PaymentEventType.Ipn, new Dictionary<string, string> { ["tran_id"] = paymentNo, ["val_id"] = "v-" + paymentNo }));
        var refundNo = (await _sql.SendCommandAsync(
            new Application.Features.Booking.Commands.CancelMyBooking.CancelMyBookingCommand(booking.BookingNo, null), customer)).Value.RefundNo!;

        await _sql.SendCommandAsync(new CompleteRefundCommand(refundNo, "BK-HALF"), await StaffAsync());

        Assert.Equal(PaymentStatus.PartiallyRefunded, Assert.Single((await AdminViewAsync(booking.BookingNo)).Payments).Status);
    }

    [Fact]
    public async Task RejectingARefund_KeepsTheReason_AndLeavesThePaymentAlone()
    {
        var booked = await PaidOnlineAsync();
        var staff = await StaffAsync();
        var refundNo = (await _sql.SendCommandAsync(new CancelBookingByAgencyCommand(booked.BookingNo, "Customer asked"), staff)).Value.RefundNo!;

        var result = await _sql.SendCommandAsync(new RejectRefundCommand(refundNo, "Refunded in cash at the office already"), staff);

        Assert.True(result.IsSuccess, result.Error.Message);
        var view = await AdminViewAsync(booked.BookingNo);
        Assert.Equal((RefundStatus.Rejected, "Refunded in cash at the office already"), (view.Refunds[0].Status, view.Refunds[0].RejectReason));
        Assert.Equal(PaymentStatus.Succeeded, view.Payments[0].Status);
    }

    [Fact]
    public async Task ALatePayment_ShowsUpInTheRefundsToProcess_AsRequestedByTheSystem()
    {
        var booked = await PendingAsync(seats: 3);
        var paymentNo = (await _sql.SendCommandAsync(new InitiatePaymentCommand(booked.BookingNo), booked.Customer)).Value.PaymentNo;
        await ExpireAsync(booked);
        await using (var scope = _sql.Services.CreateAsyncScope())
        {
            var departures = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IDepartureRepository>();
            await departures.TryReserveSeatsAsync(booked.DepartureId, 3, TestContext.Current.CancellationToken); // the seats are gone
        }
        _sql.PaymentGateway.ValidatesAs(paymentNo, booked.Total);
        await _sql.SendCommandAsync(new HandleGatewayCallbackCommand(
            PaymentEventType.Ipn, new Dictionary<string, string> { ["tran_id"] = paymentNo, ["val_id"] = "v-" + paymentNo }));

        var open = (await _sql.SendAsync(new SearchRefundsQuery(OpenOnly: true, Search: booked.BookingNo))).Value;

        var refund = Assert.Single(open.Items);
        Assert.Equal((booked.Total, paymentNo, (string?)null), (refund.Amount, refund.PaymentNo, refund.RequestedByName));
    }

    // ---------- Lists and dashboard ----------

    [Fact]
    public async Task SearchingBookings_FindsByNumberAndByMobile()
    {
        var booked = await PendingAsync();

        var byNumber = (await _sql.SendAsync(new SearchBookingsQuery(booked.BookingNo, null, null, null, null))).Value;
        var byPhone = (await _sql.SendAsync(new SearchBookingsQuery("+8801712345678", null, null, null, null))).Value; // any accepted spelling

        Assert.Equal(booked.BookingNo, Assert.Single(byNumber.Items).BookingNo);
        Assert.Contains(byPhone.Items, b => b.BookingNo == booked.BookingNo);
    }

    [Fact]
    public async Task SearchingPayments_FindsAllAttemptsOfABooking()
    {
        var booked = await PaidOnlineAsync();

        var page = (await _sql.SendAsync(new SearchPaymentsQuery(booked.BookingNo, null, null, null, null))).Value;

        Assert.Equal((PaymentStatus.Succeeded, booked.Total), (Assert.Single(page.Items).Status, page.Items[0].Amount));
    }

    [Fact]
    public async Task TheDashboard_CountsWhatHappenedToday()
    {
        var before = (await _sql.SendAsync(new GetAdminDashboardQuery())).Value;

        await PendingAsync(); // +1 booking, +1 pending payment
        var paid = await PaidOnlineAsync(); // +1 booking, +34,000 revenue
        var staff = await StaffAsync();
        var refundNo = (await _sql.SendCommandAsync(new CancelBookingByAgencyCommand(paid.BookingNo, "Called off"), staff)).Value.RefundNo!; // +1 refund to process

        var middle = (await _sql.SendAsync(new GetAdminDashboardQuery())).Value;
        Assert.Equal(before.BookingsToday + 2, middle.BookingsToday);
        Assert.Equal(before.PendingPayments + 1, middle.PendingPayments);
        Assert.Equal(before.RevenueToday + paid.Total, middle.RevenueToday);
        Assert.Equal(before.RefundsToProcess + 1, middle.RefundsToProcess);

        // The chart's last point is today, and agrees with the cards above it.
        Assert.Equal(14, middle.Last14Days.Count);
        Assert.Equal(middle.Today, middle.Last14Days[^1].Date);
        Assert.Equal(before.Last14Days[^1].Bookings + 2, middle.Last14Days[^1].Bookings);
        Assert.Equal(middle.RevenueToday, middle.Last14Days[^1].Revenue);
        // One booking still waiting, the paid one now cancelled by the agency.
        Assert.Equal(MixCount(before, BookingStatus.PendingPayment) + 1, MixCount(middle, BookingStatus.PendingPayment));
        Assert.Equal(MixCount(before, BookingStatus.Cancelled) + 1, MixCount(middle, BookingStatus.Cancelled));

        await _sql.SendCommandAsync(new CompleteRefundCommand(refundNo, "BK-1"), staff); // money back out today

        var after = (await _sql.SendAsync(new GetAdminDashboardQuery())).Value;
        Assert.Equal(before.RevenueToday, after.RevenueToday); // in and out the same day
        Assert.Equal(after.RevenueToday, after.Last14Days[^1].Revenue);
        Assert.Equal(before.RefundsToProcess, after.RefundsToProcess);
    }

    private static int MixCount(AdminDashboardDto dashboard, BookingStatus status) =>
        dashboard.StatusMix.FirstOrDefault(s => s.Status == status)?.Count ?? 0;

    // ---------- Who may do what (HTTP, real tokens) ----------

    private async Task<HttpClient> ClientAsAsync(SystemRole role)
    {
        var phone = PhoneNumber.Create("019" + Random.Shared.Next(0, 100_000_000).ToString("D8"));
        var user = User.Create($"{role} person", phone, $"{Guid.NewGuid():N}@ghuri.local", null);
        user.AssignRole(role, DateTime.UtcNow);
        await _sql.SaveAsync(user);

        var client = _sql.CreateClient();
        var token = _sql.Services.GetRequiredService<ITokenService>().CreateAccessToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    [Theory]
    [InlineData(SystemRole.Accounts, HttpStatusCode.OK)]
    [InlineData(SystemRole.Manager, HttpStatusCode.OK)]
    [InlineData(SystemRole.Sales, HttpStatusCode.Forbidden)] // Sales never touches money
    [InlineData(SystemRole.Customer, HttpStatusCode.Forbidden)]
    public async Task OnlyMoneyRoles_CanRecordAPayment(SystemRole role, HttpStatusCode expected)
    {
        var booked = await PendingAsync();
        var client = await ClientAsAsync(role);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/admin/bookings/{booked.BookingNo}/payments",
            new { amount = booked.Total, method = 1, reference = (string?)null }, TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(SystemRole.Manager, HttpStatusCode.OK)]
    [InlineData(SystemRole.Sales, HttpStatusCode.Forbidden)]    // Sales no longer sees bookings at all
    [InlineData(SystemRole.Accounts, HttpStatusCode.Forbidden)] // Accounts handles money, not customers' trips
    public async Task OnlySuperAdminAndManager_CanCancel(SystemRole role, HttpStatusCode expected)
    {
        var booked = await PendingAsync();
        var client = await ClientAsAsync(role);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/admin/bookings/{booked.BookingNo}/cancel", new { reason = "Customer phoned" }, TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    // Which admin sections each role may open (decided 2026-10-08):
    // dashboard → Super Admin · bookings, payments, refunds → not Sales ·
    // catalogue → not Accounts · custom trips → every staff member.
    [Theory]
    [InlineData("dashboard", SystemRole.SuperAdmin, HttpStatusCode.OK)]
    [InlineData("dashboard", SystemRole.Manager, HttpStatusCode.Forbidden)]
    [InlineData("dashboard", SystemRole.Sales, HttpStatusCode.Forbidden)]
    [InlineData("dashboard", SystemRole.Accounts, HttpStatusCode.Forbidden)]
    [InlineData("bookings", SystemRole.Manager, HttpStatusCode.OK)]
    [InlineData("bookings", SystemRole.Accounts, HttpStatusCode.OK)]
    [InlineData("bookings", SystemRole.Sales, HttpStatusCode.Forbidden)]
    [InlineData("payments", SystemRole.Accounts, HttpStatusCode.OK)]
    [InlineData("payments", SystemRole.Sales, HttpStatusCode.Forbidden)]
    [InlineData("refunds", SystemRole.Accounts, HttpStatusCode.OK)]
    [InlineData("refunds", SystemRole.Sales, HttpStatusCode.Forbidden)]
    [InlineData("packages", SystemRole.Manager, HttpStatusCode.OK)]
    [InlineData("packages", SystemRole.Sales, HttpStatusCode.OK)]
    [InlineData("packages", SystemRole.Accounts, HttpStatusCode.Forbidden)]
    [InlineData("destinations", SystemRole.Sales, HttpStatusCode.OK)]
    [InlineData("destinations", SystemRole.Accounts, HttpStatusCode.Forbidden)]
    [InlineData("categories", SystemRole.Sales, HttpStatusCode.OK)]
    [InlineData("categories", SystemRole.Accounts, HttpStatusCode.Forbidden)]
    [InlineData("custom-trips", SystemRole.Sales, HttpStatusCode.OK)]
    [InlineData("custom-trips", SystemRole.Accounts, HttpStatusCode.OK)]
    public async Task EachRole_OpensOnlyItsOwnSections(string section, SystemRole role, HttpStatusCode expected)
    {
        var client = await ClientAsAsync(role);

        var response = await client.GetAsync($"/api/v1/admin/{section}", TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task ACustomer_CantSeeTheAdminLists()
    {
        var client = await ClientAsAsync(SystemRole.Customer);

        var response = await client.GetAsync("/api/v1/admin/bookings", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- helpers ----------

    private async Task ExpireAsync(Booked booked)
    {
        await EndTheHoldAsync(_sql, booked.BookingNo);
        await _sql.Services.GetRequiredService<BookingExpiryJob>().RunOnceAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<Domain.Entities.Booking.BookingStatusHistory>> HistoryAsync(string bookingNo)
    {
        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = await db.Bookings.Where(b => b.BookingNo == bookingNo).Select(b => b.Id).SingleAsync();
        return await db.BookingStatusHistory.AsNoTracking().Where(h => h.BookingId == id).OrderBy(h => h.Id).ToListAsync();
    }

    private async Task EnsureGlobalPolicyAsync()
    {
        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.CancellationPolicies.AnyAsync(p => p.PackageId == null))
            return;
        db.CancellationPolicies.AddRange(
            Domain.Entities.Booking.CancellationPolicy.Create(null, 30, 100), Domain.Entities.Booking.CancellationPolicy.Create(null, 15, 50),
            Domain.Entities.Booking.CancellationPolicy.Create(null, 7, 25), Domain.Entities.Booking.CancellationPolicy.Create(null, 0, 0));
        await db.SaveChangesAsync();
    }
}
