using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBooking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// CreateBooking against real SQL Server (17-day plan, Day 8: "POST /bookings
/// works for both package types"). Through the real pipeline, so the
/// transaction commits and rolls back exactly as in production. The race for
/// the last seats and the expiry job are tested in Part 5.
/// </summary>
public class CreateBookingTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6));
    private static readonly DateTime Now = DateTime.UtcNow;

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    // ---------- Setup helpers ----------

    private async Task<Guid> NewDestinationAsync()
    {
        var unique = Unique();
        var destination = Destination.Create(sql.CountryId, $"Destination {unique}", Slug.Create($"destination-{unique}"));
        await sql.SaveAsync(destination);
        return destination.Id;
    }

    private async Task<TourPackage> NewPackageAsync(PackagePricing pricing) =>
        TourPackage.Create(
            $"T{Unique()}",
            new TourPackageDetails(await NewDestinationAsync(), "Beach Escape", Slug.Create($"beach-escape-{Unique()}"), "Summary", null,
                TourType.Group, [], [], null, null, false, null, null),
            pricing);

    /// <summary>Gives the package a cover and an itinerary, publishes it, and saves it with its departures.</summary>
    private async Task PublishAsync(TourPackage package, params Departure[] departures)
    {
        var cover = FileObject.Create($"test/{Unique()}.webp", "cover.jpg", "image/webp", 1234, new string('a', 64), true, Now);
        package.SetImages([cover.Id]);
        package.SetItinerary(Enumerable.Range(1, package.DurationDays).Select(n => new ItineraryDayDetails($"Day {n}", "Plan")).ToList());
        package.Publish(Now, hasOpenDeparture: true);
        await sql.SaveAsync([cover, package, .. departures]);
    }

    /// <summary>A published 3-day fixed package with one departure in 30 days: adult ৳12,000, child ৳9,000, infant ৳1,000.</summary>
    private async Task<(string Slug, Guid DepartureId)> FixedPackageAsync(short seats = 10)
    {
        var package = await NewPackageAsync(PackagePricing.FixedDepartures(3, 2));
        var departure = Departure.Create(package.Id, Today.AddDays(30), 3, 12_000, 9_000, 1_000, null, seats, 2);
        await PublishAsync(package, departure);
        return (package.Slug.Value, departure.Id);
    }

    /// <summary>A published flexible stay: 2-7 nights, ৳8,000 covers 2 nights, ৳3,000 each extra night, 3 days' notice.</summary>
    private async Task<string> FlexiblePackageAsync()
    {
        var package = await NewPackageAsync(PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3));
        await PublishAsync(package);
        return package.Slug.Value;
    }

    /// <summary>Two adults (one the lead), a child and an infant. A new Idempotency-Key unless one is given.</summary>
    private static CreateBookingCommand Book(
        string slug, Guid? departureId = null, DateOnly? startDate = null, int? nights = null,
        string? key = null, string? specialRequest = "Window seats, please") =>
        new(slug, departureId, startDate, nights,
            [
                new(TravellerType.Adult, "Rahim Uddin", IsLead: true),
                new(TravellerType.Adult, "Karima Begum", IsLead: false),
                new(TravellerType.Child, "Ayaan", IsLead: false),
                new(TravellerType.Infant, "Mim", IsLead: false)
            ],
            "Rahim Uddin", "01712345678", "rahim@example.com", specialRequest)
        {
            IdempotencyKey = key ?? Guid.NewGuid().ToString()
        };

    private async Task<int> BookingCountAsync(string slug)
    {
        await using var scope = sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await (from b in db.Bookings
                      join p in db.TourPackages on b.PackageId equals p.Id
                      where p.Slug == Slug.Create(slug)
                      select b).CountAsync();
    }

    // ---------- Fixed departures ----------

    [Fact]
    public async Task Fixed_BooksTheTrip_ReservesTheSeats_AndHoldsThemFor20Minutes()
    {
        var (slug, departureId) = await FixedPackageAsync(seats: 10);
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
        var (slug, departureId) = await FixedPackageAsync(seats: 2); // 3 needed

        var result = await sql.SendCommandAsync(Book(slug, departureId), await sql.NewCustomerAsync());

        Assert.Equal("not_enough_seats", result.Error.Code);
        Assert.Equal(0, await sql.ReservedSeatsAsync(departureId));
        Assert.Equal(0, await BookingCountAsync(slug));
    }

    // ---------- Flexible stays ----------

    [Fact]
    public async Task Flexible_BooksTheStay_WithoutAnySeats()
    {
        var slug = await FlexiblePackageAsync();
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
        var slug = await FlexiblePackageAsync();

        var result = await sql.SendCommandAsync(Book(slug, startDate: Today.AddDays(1), nights: 3), await sql.NewCustomerAsync());

        Assert.Equal("start_date_too_soon", result.Error.Code);
        Assert.Equal(0, await BookingCountAsync(slug));
    }

    // ---------- Idempotency-Key ----------

    [Fact]
    public async Task SameKeyTwice_ReturnsTheSameBooking_AndReservesTheSeatsOnce()
    {
        var (slug, departureId) = await FixedPackageAsync();
        var customer = await sql.NewCustomerAsync();
        var command = Book(slug, departureId);

        var first = await sql.SendCommandAsync(command, customer);
        var retry = await sql.SendCommandAsync(command, customer); // a double-click, a resent request

        Assert.True(retry.IsSuccess, retry.Error.Message);
        Assert.Equal(first.Value, retry.Value); // the very same answer
        Assert.Equal(1, await BookingCountAsync(slug));
        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId)); // not 6
    }

    [Fact]
    public async Task SameKey_WithADifferentRequest_IsRefused()
    {
        var (slug, departureId) = await FixedPackageAsync();
        var customer = await sql.NewCustomerAsync();
        var key = Guid.NewGuid().ToString();
        await sql.SendCommandAsync(Book(slug, departureId, key: key), customer);

        var reused = await sql.SendCommandAsync(Book(slug, departureId, key: key, specialRequest: "Something else"), customer);

        Assert.Equal("idempotency_key_reused", reused.Error.Code);
        Assert.Equal(1, await BookingCountAsync(slug));
        Assert.Equal(3, await sql.ReservedSeatsAsync(departureId));
    }

    [Fact]
    public async Task SameKey_FromAnotherCustomer_IsRefused()
    {
        var (slug, departureId) = await FixedPackageAsync();
        var key = Guid.NewGuid().ToString();
        await sql.SendCommandAsync(Book(slug, departureId, key: key), await sql.NewCustomerAsync());

        var other = await sql.SendCommandAsync(Book(slug, departureId, key: key), await sql.NewCustomerAsync());

        Assert.Equal("idempotency_key_reused", other.Error.Code);
    }

    // ---------- Who can see / make a booking ----------

    [Fact]
    public async Task GetMyBooking_SomeoneElsesBooking_IsNotFound()
    {
        var (slug, departureId) = await FixedPackageAsync();
        var created = await sql.SendCommandAsync(Book(slug, departureId), await sql.NewCustomerAsync());

        var stranger = await sql.SendAsync(new GetMyBookingQuery(created.Value.BookingNo), await sql.NewCustomerAsync());

        Assert.Equal("booking_not_found", stranger.Error.Code);
    }

    [Fact]
    public async Task Http_TheHeaderIsRequired_AndABookingAnswers201WithItsAddress()
    {
        // The whole path a browser takes: a real token, JSON body, the header.
        var (slug, departureId) = await FixedPackageAsync();
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
            contactPhone = "01712345678"
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
        var (slug, departureId) = await FixedPackageAsync();

        var result = await sql.SendCommandAsync(Book(slug, departureId), asUser: null);

        Assert.Equal("not_authenticated", result.Error.Code);
        Assert.Equal(0, await sql.ReservedSeatsAsync(departureId));
    }
}
