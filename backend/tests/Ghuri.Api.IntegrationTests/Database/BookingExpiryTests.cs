using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Domain.Enums;
using Ghuri.Infrastructure.Jobs;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// The expiry job against real SQL Server (17-day plan, Day 8: "expire
/// booking, release seats for fixed departures"). Seats are money: a hold
/// that never ends would sell a date out for nothing.
/// </summary>
/// <remarks>
/// The job's timer is off in tests (GhuriApiFactory); each test calls
/// RunOnceAsync itself. "20 minutes passing" is done by moving the booking's
/// HoldExpiresAtUtc into the past - the job reads the real clock.
/// </remarks>
public class BookingExpiryTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private BookingExpiryJob Job => sql.Services.GetRequiredService<BookingExpiryJob>();

    private async Task<CreateBookingResponse> BookAsync(CreateBookingCommand command)
    {
        var result = await sql.SendCommandAsync(command, await sql.NewCustomerAsync());
        Assert.True(result.IsSuccess, result.Error.Message);
        return result.Value;
    }

    /// <summary>The booking's status, its hold, and its newest history row - straight from the database.</summary>
    private async Task<(BookingStatus Status, DateTime? Hold, BookingStatus LastTo, Guid? LastBy)> StateAsync(string bookingNo)
    {
        await using var scope = sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.AsNoTracking().SingleAsync(b => b.BookingNo == bookingNo);
        var last = await db.BookingStatusHistory.AsNoTracking()
            .Where(h => h.BookingId == booking.Id)
            .OrderByDescending(h => h.Id)
            .FirstAsync();
        return (booking.Status, booking.HoldExpiresAtUtc, last.ToStatus, last.ChangedBy);
    }

    [Fact]
    public async Task AnUnpaidFixedBooking_Expires_AndItsSeatsComeBack()
    {
        var (slug, departureId) = await FixedPackageAsync(sql, seats: 10);
        var booking = await BookAsync(Book(slug, departureId));
        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId));
        await EndTheHoldAsync(sql, booking.BookingNo);

        await Job.RunOnceAsync(TestContext.Current.CancellationToken);

        var state = await StateAsync(booking.BookingNo);
        Assert.Equal(BookingStatus.Expired, state.Status);
        Assert.Null(state.Hold);
        Assert.Equal((BookingStatus.Expired, (Guid?)null), (state.LastTo, state.LastBy)); // recorded, by "nobody" = the job
        Assert.Equal(0, await sql.ReservedSeatsAsync(departureId));
    }

    [Fact]
    public async Task RunningAgain_DoesNotGiveTheSeatsBackTwice()
    {
        var (slug, departureId) = await FixedPackageAsync(sql, seats: 10);
        var expired = await BookAsync(Book(slug, departureId));
        await BookAsync(Book(slug, departureId)); // a second, still-valid booking: 3 more seats
        await EndTheHoldAsync(sql, expired.BookingNo);

        await Job.RunOnceAsync(TestContext.Current.CancellationToken);
        await Job.RunOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId)); // only the valid booking's seats remain
    }

    [Fact]
    public async Task ABookingStillInsideItsWindow_IsLeftAlone()
    {
        var (slug, departureId) = await FixedPackageAsync(sql, seats: 10);
        var booking = await BookAsync(Book(slug, departureId));

        await Job.RunOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.PendingPayment, (await StateAsync(booking.BookingNo)).Status);
        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId));
    }

    [Fact]
    public async Task AnUnpaidFlexibleStay_Expires_WithNoSeatsToGiveBack()
    {
        var slug = await FlexiblePackageAsync(sql);
        var booking = await BookAsync(Book(slug, startDate: Today.AddDays(10), nights: 3));
        await EndTheHoldAsync(sql, booking.BookingNo);

        await Job.RunOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Expired, (await StateAsync(booking.BookingNo)).Status);
    }
}
