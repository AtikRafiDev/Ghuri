using Ghuri.Domain.Repositories;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// The plan's mandatory test (Day 5): seats are money. 20 customers
/// clicking "Book" on the last 5 seats at the same moment must give
/// exactly 5 bookings - never 6, never a negative seat count.
/// </summary>
public class SeatReservationTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>One request = its own scope = its own DbContext and database connection, like a real HTTP request.</summary>
    private async Task<bool> ReserveAsync(Guid departureId, short seats)
    {
        await using var scope = sql.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IDepartureRepository>().TryReserveSeatsAsync(departureId, seats, Ct);
    }

    private async Task<bool> ReleaseAsync(Guid departureId, short seats)
    {
        await using var scope = sql.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IDepartureRepository>().ReleaseSeatsAsync(departureId, seats, Ct);
    }

    [Fact]
    public async Task TwentyParallelReservations_ForFiveSeats_ExactlyFiveSucceed()
    {
        var departureId = await sql.NewDepartureAsync(totalSeats: 5);

        // A starting gate: all 20 tasks get ready and wait, then are released
        // together - so they really race, instead of running one after another.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = Enumerable.Range(0, 20).Select(async _ =>
        {
            await gate.Task;
            return await ReserveAsync(departureId, 1);
        }).ToList();
        gate.SetResult();
        var results = await Task.WhenAll(attempts);

        Assert.Equal(5, results.Count(taken => taken));
        Assert.Equal(15, results.Count(taken => !taken));
        Assert.Equal(5, await sql.ReservedSeatsAsync(departureId));
    }

    [Fact]
    public async Task Reserve_MoreThanWhatIsLeft_FailsAndTakesNothing()
    {
        var departureId = await sql.NewDepartureAsync(totalSeats: 5);

        Assert.True(await ReserveAsync(departureId, 3));
        Assert.False(await ReserveAsync(departureId, 3)); // only 2 left - all or nothing
        Assert.True(await ReserveAsync(departureId, 2));
        Assert.Equal(5, await sql.ReservedSeatsAsync(departureId));
    }

    [Fact]
    public async Task Reserve_OnAClosedDeparture_IsRefused()
    {
        var departureId = await sql.NewDepartureAsync(totalSeats: 20, closed: true);

        Assert.False(await ReserveAsync(departureId, 1));
        Assert.Equal(0, await sql.ReservedSeatsAsync(departureId));
    }

    [Fact]
    public async Task Release_NeverGoesBelowZero()
    {
        var departureId = await sql.NewDepartureAsync(totalSeats: 10);
        await ReserveAsync(departureId, 2);

        Assert.False(await ReleaseAsync(departureId, 3)); // more than were taken
        Assert.Equal(2, await sql.ReservedSeatsAsync(departureId));

        Assert.True(await ReleaseAsync(departureId, 2));
        Assert.Equal(0, await sql.ReservedSeatsAsync(departureId));
    }

    [Fact]
    public async Task Database_RefusesMoreReservedThanTotal_EvenWithoutTheRepository()
    {
        // The last line of defence: CK_Departures_ReservedSeats in SQL itself.
        var departureId = await sql.NewDepartureAsync(totalSeats: 2);
        await using var scope = sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var error = await Assert.ThrowsAnyAsync<Exception>(() => db.Departures
            .Where(d => d.Id == departureId)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.ReservedSeats, (short)3), Ct));

        Assert.Contains("CK_Departures_ReservedSeats", error.ToString());
    }
}
