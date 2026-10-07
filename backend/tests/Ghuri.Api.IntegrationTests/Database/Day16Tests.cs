using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Day 16 (hardening) against real SQL Server: one account can't hold
/// every seat.
/// </summary>
public class Day16Tests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task OneCustomer_CanHoldAtMostThreeUnpaidBookings()
    {
        var (slug, departureId) = await FixedPackageAsync(sql, seats: 40);
        var customer = await sql.NewCustomerAsync();
        CreateBookingCommand? third = null;
        for (var i = 0; i < 3; i++)
        {
            third = Book(slug, departureId);
            Assert.True((await sql.SendCommandAsync(third, customer)).IsSuccess);
        }

        var fourth = await sql.SendCommandAsync(Book(slug, departureId), customer);
        var resent = await sql.SendCommandAsync(third!, customer); // a retry of an existing booking still gets its answer

        Assert.Equal("too_many_unpaid_bookings", fourth.Error.Code);
        Assert.True(resent.IsSuccess, resent.Error.Message);
        Assert.Equal(9, await sql.ReservedSeatsAsync(departureId)); // 3 bookings × 3 seats - the 4th held nothing

        var someoneElse = await sql.SendCommandAsync(Book(slug, departureId), await sql.NewCustomerAsync());
        Assert.True(someoneElse.IsSuccess, someoneElse.Error.Message); // the limit is per customer
    }

    [Fact]
    public async Task OnceAHoldEnds_ItNoLongerCounts()
    {
        var (slug, departureId) = await FixedPackageAsync(sql, seats: 40);
        var customer = await sql.NewCustomerAsync();
        var bookingNos = new List<string>();
        for (var i = 0; i < 3; i++)
            bookingNos.Add((await sql.SendCommandAsync(Book(slug, departureId), customer)).Value.BookingNo);

        await EndTheHoldAsync(sql, bookingNos[0]); // 20 minutes passed for one of them

        Assert.True((await sql.SendCommandAsync(Book(slug, departureId), customer)).IsSuccess);
    }
}
