using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBooking;
using Ghuri.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// CreateBooking against real SQL Server (17-day plan, Day 8: "POST /bookings
/// works for both package types"). Through the real pipeline, so the
/// transaction commits and rolls back exactly as in production. The expiry
/// job has its own tests (BookingExpiryTests); the race for the last seats
/// is Part 5.
/// </summary>
public class CreateBookingTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    // ---------- Fixed departures ----------

    [Fact]
    public async Task Fixed_BooksTheTrip_ReservesTheSeats_AndHoldsThemFor20Minutes()
    {
        var (slug, departureId) = await FixedPackageAsync(sql, seats: 10);
        var customer = await sql.NewCustomerAsync();

        var result = await sql.SendCommandAsync(Book(slug, departureId), customer);

        Assert.True(result.IsSuccess, result.Error.Message);
        var created = result.Value;
        Assert.StartsWith("TB", created.BookingNo);
        Assert.Equal(34_000, created.TotalAmount); // 2 × 12,000 + 9,000 + 1,000
        Assert.InRange(created.HoldExpiresAtUtc, Now.AddMinutes(19), Now.AddMinutes(21));
        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId)); // 2 adults + 1 child; the infant sits on a lap

        var mine = await sql.SendAsync(new GetMyBookingQuery(created.BookingNo), customer);
        Assert.True(mine.IsSuccess, mine.Error.Message);
        Assert.Equal((BookingType.FixedDeparture, BookingStatus.PendingPayment, slug),
            (mine.Value.BookingType, mine.Value.Status, mine.Value.PackageSlug));
        Assert.Equal(4, mine.Value.Travellers.Count);
        Assert.True(mine.Value.Travellers[0].IsLead); // the lead comes first
        Assert.Equal("Window seats, please", mine.Value.SpecialRequest);
    }

    [Fact]
    public async Task Fixed_NotEnoughSeats_IsRefused_AndNothingIsTaken()
    {
        var (slug, departureId) = await FixedPackageAsync(sql, seats: 2); // 3 needed

        var result = await sql.SendCommandAsync(Book(slug, departureId), await sql.NewCustomerAsync());

        Assert.Equal("not_enough_seats", result.Error.Code);
        Assert.Equal(0, await sql.ReservedSeatsAsync(departureId));
        Assert.Equal(0, await BookingCountAsync(sql, slug));
    }

    // ---------- Flexible stays ----------

    [Fact]
    public async Task Flexible_BooksTheStay_WithoutAnySeats()
    {
        var slug = await FlexiblePackageAsync(sql);
        var customer = await sql.NewCustomerAsync();
        var checkIn = Today.AddDays(10);

        var result = await sql.SendCommandAsync(Book(slug, startDate: checkIn, nights: 4), customer);

        Assert.True(result.IsSuccess, result.Error.Message);
        // Per person: 8,000 + 2 extra nights × 3,000 = 14,000. 2 adults + 1 child; infant free = 42,000.
        Assert.Equal(42_000, result.Value.TotalAmount);
        var mine = (await sql.SendAsync(new GetMyBookingQuery(result.Value.BookingNo), customer)).Value;
        Assert.Equal((BookingType.FlexibleStay, checkIn, checkIn.AddDays(4), 4), (mine.BookingType, mine.StartDate, mine.EndDate, mine.Nights));
    }

    [Fact]
    public async Task Flexible_StartingTooSoon_IsRefused_AndNothingIsSaved()
    {
        var slug = await FlexiblePackageAsync(sql);

        var result = await sql.SendCommandAsync(Book(slug, startDate: Today.AddDays(1), nights: 3), await sql.NewCustomerAsync());

        Assert.Equal("start_date_too_soon", result.Error.Code);
        Assert.Equal(0, await BookingCountAsync(sql, slug));
    }

    // ---------- Idempotency-Key ----------

    [Fact]
    public async Task SameKeyTwice_ReturnsTheSameBooking_AndReservesTheSeatsOnce()
    {
        var (slug, departureId) = await FixedPackageAsync(sql);
        var customer = await sql.NewCustomerAsync();
        var command = Book(slug, departureId);

        var first = await sql.SendCommandAsync(command, customer);
        var retry = await sql.SendCommandAsync(command, customer); // a double-click, a resent request

        Assert.True(retry.IsSuccess, retry.Error.Message);
        Assert.Equal(first.Value, retry.Value); // the very same answer
        Assert.Equal(1, await BookingCountAsync(sql, slug));
        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId)); // not 6
    }

    [Fact]
    public async Task SameKey_WithADifferentRequest_IsRefused()
    {
        var (slug, departureId) = await FixedPackageAsync(sql);
        var customer = await sql.NewCustomerAsync();
        var key = Guid.NewGuid().ToString();
        await sql.SendCommandAsync(Book(slug, departureId, key: key), customer);

        var reused = await sql.SendCommandAsync(Book(slug, departureId, key: key, specialRequest: "Something else"), customer);

        Assert.Equal("idempotency_key_reused", reused.Error.Code);
        Assert.Equal(1, await BookingCountAsync(sql, slug));
        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId));
    }

    [Fact]
    public async Task SameKey_FromAnotherCustomer_IsRefused()
    {
        var (slug, departureId) = await FixedPackageAsync(sql);
        var key = Guid.NewGuid().ToString();
        await sql.SendCommandAsync(Book(slug, departureId, key: key), await sql.NewCustomerAsync());

        var other = await sql.SendCommandAsync(Book(slug, departureId, key: key), await sql.NewCustomerAsync());

        Assert.Equal("idempotency_key_reused", other.Error.Code);
    }

    // ---------- Who can see / make a booking ----------

    [Fact]
    public async Task GetMyBooking_SomeoneElsesBooking_IsNotFound()
    {
        var (slug, departureId) = await FixedPackageAsync(sql);
        var created = await sql.SendCommandAsync(Book(slug, departureId), await sql.NewCustomerAsync());

        var stranger = await sql.SendAsync(new GetMyBookingQuery(created.Value.BookingNo), await sql.NewCustomerAsync());

        Assert.Equal("booking_not_found", stranger.Error.Code);
    }

    [Fact]
    public async Task Http_TheHeaderIsRequired_AndABookingAnswers201WithItsAddress()
    {
        // The whole path a browser takes: a real token, JSON body, the header.
        var (slug, departureId) = await FixedPackageAsync(sql);
        var customer = await sql.NewCustomerUserAsync();
        var client = sql.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", sql.Services.GetRequiredService<ITokenService>().CreateAccessToken(customer).Value);
        var body = new
        {
            packageSlug = slug,
            departureId,
            travellers = new[] { new { type = 1, fullName = "Rahim Uddin", isLead = true } },
            contactName = "Rahim Uddin",
            contactPhone = "01712345678",
            contactEmail = "rahim@example.com"
        };

        var noHeader = await client.PostAsJsonAsync("/api/v1/bookings", body, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, noHeader.StatusCode);
        Assert.Contains("IdempotencyKey", await noHeader.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/bookings") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var created = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.StartsWith("/api/v1/bookings/TB", created.Headers.Location?.OriginalString);

        var mine = await client.GetAsync(created.Headers.Location, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Equal(1, await sql.ReservedSeatsAsync(departureId));
    }

    [Fact]
    public async Task NotLoggedIn_IsRefused()
    {
        var (slug, departureId) = await FixedPackageAsync(sql);

        var result = await sql.SendCommandAsync(Book(slug, departureId), asUser: null);

        Assert.Equal("not_authenticated", result.Error.Code);
        Assert.Equal(0, await sql.ReservedSeatsAsync(departureId));
    }
}
