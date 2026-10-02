using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Domain.Enums;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Many requests at the same moment, against real SQL Server (17-day plan,
/// Day 8 - "double bookings are impossible"). The plan marks these as tests
/// that are never skipped: they are where money and seats get lost.
/// </summary>
/// <remarks>
/// The requests really run at once: every task is started before any is
/// awaited, each with its own scope, DbContext and transaction - like
/// separate customers' browsers. Day 5's SeatReservationTests prove the
/// atomic UPDATE on its own; these prove the whole booking around it.
/// </remarks>
public class BookingConcurrencyTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    /// <summary>One adult = one seat, so "3 seats" means exactly 3 bookings can succeed.</summary>
    private static CreateBookingCommand BookOneSeat(string slug, Guid departureId) =>
        Book(slug, departureId) with { Travellers = [new(TravellerType.Adult, "Solo Traveller", IsLead: true)] };

    [Fact]
    public async Task TenCustomers_ForTheLastThreeSeats_ExactlyThreeGetABooking()
    {
        var (slug, departureId) = await FixedPackageAsync(sql, seats: 3);
        var customers = new List<Guid>();
        for (var i = 0; i < 10; i++)
            customers.Add(await sql.NewCustomerAsync());

        // All ten start before any is awaited - they hit the database together.
        var results = await Task.WhenAll(customers.Select(customer =>
            Task.Run(() => sql.SendCommandAsync(BookOneSeat(slug, departureId), customer))));

        Assert.Equal(3, results.Count(r => r.IsSuccess));
        // Everyone else gets the friendly "not enough seats" - never a crash
        // or a deadlock (whether the pre-check or the atomic UPDATE said no).
        Assert.All(results.Where(r => r.IsFailure), r => Assert.Equal("not_enough_seats", r.Error.Code));
        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId));
        Assert.Equal(3, await BookingCountAsync(sql, slug));
        Assert.Equal(3, results.Where(r => r.IsSuccess).Select(r => r.Value.BookingNo).Distinct().Count()); // 3 different numbers
    }

    [Fact]
    public async Task TheSameRequestFiveTimesAtOnce_MakesOneBooking()
    {
        // A customer hammering "Book now" on a slow connection: five copies
        // of the same request, same Idempotency-Key, all at the same moment.
        var (slug, departureId) = await FixedPackageAsync(sql, seats: 10);
        var customer = await sql.NewCustomerAsync();
        var command = Book(slug, departureId); // 3 seats, one key

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            Task.Run(() => sql.SendCommandAsync(command, customer))));

        // The first one books; the other four wait on the key's lock, then
        // get the SAME answer back.
        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error.Message));
        Assert.Single(results.Select(r => r.Value.BookingId).Distinct());
        Assert.Equal(1, await BookingCountAsync(sql, slug));
        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId)); // not 15
    }
}
