using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Ready-made packages and bookings for the booking tests (CreateBookingTests,
/// BookingExpiryTests). Every package gets unique names, so tests sharing one
/// database never see each other's rows.
/// </summary>
internal static class BookingTestData
{
    public static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6));
    public static readonly DateTime Now = DateTime.UtcNow;

    public static string Unique() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>A published 3-day fixed package with one departure in 30 days: adult ৳12,000, child ৳9,000, infant ৳1,000.</summary>
    public static async Task<(string Slug, Guid DepartureId)> FixedPackageAsync(SqlServerFixture sql, short seats = 10)
    {
        var package = await NewPackageAsync(sql, PackagePricing.FixedDepartures(3, 2));
        var departure = Departure.Create(package.Id, Today.AddDays(30), 3, 12_000, 9_000, 1_000, null, seats, 2);
        await PublishAsync(sql, package, departure);
        return (package.Slug.Value, departure.Id);
    }

    /// <summary>A published flexible stay: 2-7 nights, ৳8,000 covers 2 nights, ৳3,000 each extra night, 3 days' notice.</summary>
    public static async Task<string> FlexiblePackageAsync(SqlServerFixture sql)
    {
        var package = await NewPackageAsync(sql, PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3));
        await PublishAsync(sql, package);
        return package.Slug.Value;
    }

    /// <summary>Two adults (one the lead), a child and an infant - 3 seats. A new Idempotency-Key unless one is given.</summary>
    public static CreateBookingCommand Book(
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

    /// <summary>How many bookings this package has, straight from the database.</summary>
    public static async Task<int> BookingCountAsync(SqlServerFixture sql, string slug)
    {
        await using var scope = sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await (from b in db.Bookings
                      join p in db.TourPackages on b.PackageId equals p.Id
                      where p.Slug == Slug.Create(slug)
                      select b).CountAsync();
    }

    private static async Task<TourPackage> NewPackageAsync(SqlServerFixture sql, PackagePricing pricing)
    {
        var unique = Unique();
        var destination = Destination.Create(sql.CountryId, $"Destination {unique}", Slug.Create($"destination-{unique}"));
        await sql.SaveAsync(destination);

        return TourPackage.Create(
            $"T{Unique()}",
            new TourPackageDetails(destination.Id, "Beach Escape", Slug.Create($"beach-escape-{Unique()}"), "Summary", null,
                TourType.Group, [], [], null, null, false, null, null),
            pricing);
    }

    /// <summary>Gives the package a cover and an itinerary, publishes it, and saves it with its departures.</summary>
    private static async Task PublishAsync(SqlServerFixture sql, TourPackage package, params Departure[] departures)
    {
        var cover = FileObject.Create($"test/{Unique()}.webp", "cover.jpg", "image/webp", 1234, new string('a', 64), true, Now);
        package.SetImages([cover.Id]);
        package.SetItinerary(Enumerable.Range(1, package.DurationDays).Select(n => new ItineraryDayDetails($"Day {n}", "Plan")).ToList());
        package.Publish(Now, hasOpenDeparture: true);
        await sql.SaveAsync([cover, package, .. departures]);
    }
}
