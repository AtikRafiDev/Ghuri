using Ghuri.Application.Features.Booking.Commands.CancelMyBooking;
using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBookings;
using Ghuri.Application.Features.Payments.Commands.HandleGatewayCallback;
using Ghuri.Application.Features.Payments.Commands.InitiatePayment;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Enums;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// A customer cancelling their own booking, against real SQL Server
/// (17-day plan, Day 11: CancelBookingByCustomer). Money: the refund is
/// exactly what the policy says for the days left, and seats always go back.
/// </summary>
/// <remarks>
/// The global policy used here is the seeded one: 30+ days 100% · 15+ 50% ·
/// 7+ 25% · less 0%. Flexible stays are used for the "days before" cases,
/// because their start date can be chosen.
/// </remarks>
public class CancelMyBookingTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _sql;

    public CancelMyBookingTests(SqlServerFixture sql)
    {
        _sql = sql;
        _sql.PaymentGateway.Reset();
    }

    private async Task EnsureGlobalPolicyAsync()
    {
        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.CancellationPolicies.AnyAsync(p => p.PackageId == null))
            return;
        db.CancellationPolicies.AddRange(
            CancellationPolicy.Create(null, 30, 100), CancellationPolicy.Create(null, 15, 50),
            CancellationPolicy.Create(null, 7, 25), CancellationPolicy.Create(null, 0, 0));
        await db.SaveChangesAsync();
    }

    private sealed record Booked(Guid Customer, string BookingNo, decimal Total, string? PaymentNo);

    private async Task<Booked> BookAsync(CreateBookingCommand command)
    {
        var customer = await _sql.NewCustomerAsync();
        var booking = await _sql.SendCommandAsync(command, customer);
        Assert.True(booking.IsSuccess, booking.Error.Message);
        return new Booked(customer, booking.Value.BookingNo, booking.Value.TotalAmount, null);
    }

    /// <summary>A booking paid the real way: payment started, SSLCommerz (fake) validates it, the booking confirms.</summary>
    private async Task<Booked> PaidAsync(CreateBookingCommand command)
    {
        var booked = await BookAsync(command);
        var paymentNo = (await _sql.SendCommandAsync(new InitiatePaymentCommand(booked.BookingNo), booked.Customer)).Value.PaymentNo;
        _sql.PaymentGateway.ValidatesAs(paymentNo, booked.Total);
        var confirmed = await _sql.SendCommandAsync(new HandleGatewayCallbackCommand(
            PaymentEventType.Ipn, new Dictionary<string, string> { ["tran_id"] = paymentNo, ["val_id"] = "v-" + paymentNo }));
        Assert.True(confirmed.IsSuccess, confirmed.Error.Message);
        return booked with { PaymentNo = paymentNo };
    }

    private async Task<Booked> PaidFlexibleAsync(int daysAhead)
    {
        await EnsureGlobalPolicyAsync();
        var slug = await FlexiblePackageAsync(_sql);
        return await PaidAsync(Book(slug, startDate: Today.AddDays(daysAhead), nights: 4));
    }

    private async Task<(BookingStatus Status, List<Domain.Entities.Payment.Refund> Refunds)> StateAsync(string bookingNo)
    {
        await using var scope = _sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.AsNoTracking().SingleAsync(b => b.BookingNo == bookingNo);
        var refunds = await db.Refunds.AsNoTracking().Where(r => r.BookingId == booking.Id).ToListAsync();
        return (booking.Status, refunds);
    }

    // ---------- Unpaid ----------

    [Fact]
    public async Task AnUnpaidBooking_IsCancelled_ItsSeatsComeBack_AndNothingIsRefunded()
    {
        var (slug, departureId) = await FixedPackageAsync(_sql, seats: 10);
        var booked = await BookAsync(Book(slug, departureId));
        Assert.Equal(3, await _sql.ReservedSeatsAsync(departureId));

        var result = await _sql.SendCommandAsync(new CancelMyBookingCommand(booked.BookingNo, "  Plans changed  "), booked.Customer);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal((0m, 0m, (string?)null), (result.Value.RefundPercent, result.Value.RefundAmount, result.Value.RefundNo));
        var (status, refunds) = await StateAsync(booked.BookingNo);
        Assert.Equal(BookingStatus.Cancelled, status);
        Assert.Empty(refunds);
        Assert.Equal(0, await _sql.ReservedSeatsAsync(departureId));
    }

    // ---------- Paid: the policy decides ----------

    [Theory]
    [InlineData(40, 100)]
    [InlineData(30, 100)] // exactly 30 days still counts as "30+"
    [InlineData(20, 50)]
    [InlineData(10, 25)]
    public async Task APaidBooking_IsRefunded_ByHowManyDaysAreLeft(int daysAhead, int expectedPercent)
    {
        var booked = await PaidFlexibleAsync(daysAhead);
        var expected = booked.Total * expectedPercent / 100m;

        var result = await _sql.SendCommandAsync(new CancelMyBookingCommand(booked.BookingNo, null), booked.Customer);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal((expectedPercent, expected), ((int)result.Value.RefundPercent, result.Value.RefundAmount));
        Assert.StartsWith("RF", result.Value.RefundNo);

        var (status, refunds) = await StateAsync(booked.BookingNo);
        Assert.Equal(BookingStatus.Cancelled, status);
        var refund = Assert.Single(refunds);
        Assert.Equal((result.Value.RefundNo, expected, RefundStatus.Requested, booked.Customer, "Cancelled by the customer."),
            (refund.RefundNo, refund.Amount, refund.Status, refund.RequestedBy, refund.Reason));
    }

    [Fact]
    public async Task APaidBooking_TooCloseToTheTrip_IsCancelled_WithoutARefund()
    {
        var booked = await PaidFlexibleAsync(daysAhead: 5); // 0% under 7 days

        var result = await _sql.SendCommandAsync(new CancelMyBookingCommand(booked.BookingNo, null), booked.Customer);

        Assert.Equal((0m, (string?)null), (result.Value.RefundAmount, result.Value.RefundNo));
        var (status, refunds) = await StateAsync(booked.BookingNo);
        Assert.Equal(BookingStatus.Cancelled, status);
        Assert.Empty(refunds); // a ৳0 refund row would only confuse staff
    }

    [Fact]
    public async Task APackagesOwnRules_WinOverTheGlobalOnes()
    {
        await EnsureGlobalPolicyAsync();
        var slug = await FlexiblePackageAsync(_sql);
        await using (var scope = _sql.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var packageId = await db.TourPackages.Where(p => p.Slug == Domain.ValueObjects.Slug.Create(slug)).Select(p => p.Id).SingleAsync();
            db.CancellationPolicies.Add(CancellationPolicy.Create(packageId, 0, 80)); // generous: 80% whenever
            await db.SaveChangesAsync();
        }
        var booked = await PaidAsync(Book(slug, startDate: Today.AddDays(5), nights: 4)); // globally that would be 0%

        var result = await _sql.SendCommandAsync(new CancelMyBookingCommand(booked.BookingNo, null), booked.Customer);

        Assert.Equal((80m, booked.Total * 0.8m), (result.Value.RefundPercent, result.Value.RefundAmount));
    }

    // ---------- Refused ----------

    [Fact]
    public async Task SomeoneElsesBooking_IsNotFound()
    {
        var booked = await PaidFlexibleAsync(daysAhead: 40);

        var result = await _sql.SendCommandAsync(new CancelMyBookingCommand(booked.BookingNo, null), await _sql.NewCustomerAsync());

        Assert.Equal("booking_not_found", result.Error.Code);
        Assert.Equal(BookingStatus.Confirmed, (await StateAsync(booked.BookingNo)).Status);
    }

    [Fact]
    public async Task CancellingTwice_IsRefused_AndRefundsOnce()
    {
        var booked = await PaidFlexibleAsync(daysAhead: 40);
        await _sql.SendCommandAsync(new CancelMyBookingCommand(booked.BookingNo, null), booked.Customer);

        var again = await _sql.SendCommandAsync(new CancelMyBookingCommand(booked.BookingNo, null), booked.Customer);

        Assert.Equal("booking_not_cancellable", again.Error.Code);
        Assert.Single((await StateAsync(booked.BookingNo)).Refunds);
    }

    [Fact]
    public async Task OnceTheTripHasStarted_ItCantBeCancelledOnline()
    {
        var booked = await PaidFlexibleAsync(daysAhead: 40);
        await using (var scope = _sql.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Bookings
                .Where(b => b.BookingNo == booked.BookingNo)
                .ExecuteUpdateAsync(set => set.SetProperty(b => b.StartDate, Today)); // "today is the first day"
        }

        var result = await _sql.SendCommandAsync(new CancelMyBookingCommand(booked.BookingNo, null), booked.Customer);

        Assert.Equal("booking_not_cancellable", result.Error.Code);
        Assert.Contains("already started", result.Error.Message);
    }

    // ---------- What the customer sees ----------

    [Fact]
    public async Task TheBookingPage_ShowsTheRefundBeforeCancelling_AndTheRefundAfter()
    {
        var booked = await PaidFlexibleAsync(daysAhead: 20);

        var before = (await _sql.SendAsync(new GetMyBookingQuery(booked.BookingNo), booked.Customer)).Value;
        Assert.Equal((true, 50m, booked.Total / 2), (before.Cancellation.CanCancel, before.Cancellation.RefundPercent, before.Cancellation.RefundAmount));
        Assert.Null(before.Refund);

        var cancelled = await _sql.SendCommandAsync(new CancelMyBookingCommand(booked.BookingNo, null), booked.Customer);

        var after = (await _sql.SendAsync(new GetMyBookingQuery(booked.BookingNo), booked.Customer)).Value;
        Assert.Equal(BookingStatus.Cancelled, after.Status);
        Assert.False(after.Cancellation.CanCancel);
        Assert.Equal((cancelled.Value.RefundNo, booked.Total / 2, RefundStatus.Requested), (after.Refund!.RefundNo, after.Refund.Amount, after.Refund.Status));
        Assert.NotNull(after.CancelledAtUtc);
    }

    [Fact]
    public async Task MyBookings_ListsOnlyMine_NewestFirst()
    {
        var (slug, departureId) = await FixedPackageAsync(_sql);
        var customer = await _sql.NewCustomerAsync();
        var first = (await _sql.SendCommandAsync(Book(slug, departureId), customer)).Value.BookingNo;
        var second = (await _sql.SendCommandAsync(Book(slug, departureId), customer)).Value.BookingNo;
        await _sql.SendCommandAsync(Book(slug, departureId), await _sql.NewCustomerAsync()); // someone else's

        var mine = (await _sql.SendAsync(new GetMyBookingsQuery(), customer)).Value;

        Assert.Equal(new[] { second, first }, mine.Select(b => b.BookingNo));
        Assert.All(mine, b => Assert.Equal((4, BookingStatus.PendingPayment), (b.Travellers, b.Status)));
    }
}
