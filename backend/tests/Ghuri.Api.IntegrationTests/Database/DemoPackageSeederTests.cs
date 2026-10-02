using Ghuri.Application.Features.Booking.Commands.CreateBooking;
using Ghuri.Application.Features.Booking.Queries.GetBookingQuote;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Persistence;
using Ghuri.Infrastructure.Persistence.Seed;
using Ghuri.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// The demo packages from "seed-demo" really are publishable and bookable -
/// they go through the same domain rules as the admin, so a seeding mistake
/// (a missing cover, an itinerary too short) fails here instead of on your PC.
/// </summary>
public class DemoPackageSeederTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private async Task<int> SeedAsync()
    {
        await using var scope = sql.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<DemoPackageSeeder>().SeedAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SeedsFourPublishedPackages_WithPhotosOnDisk_AndTheyCanBeBooked()
    {
        // The demo destinations the packages belong to ("Cox's Bazar" comes with the fixture).
        await sql.SaveAsync(
            Destination.Create(sql.CountryId, "Sajek Valley", Slug.Create("Sajek Valley")),
            Destination.Create(sql.CountryId, "Saint Martin's Island", Slug.Create("Saint Martin's Island")),
            Destination.Create(sql.CountryId, "Sylhet", Slug.Create("Sylhet")),
            Category.Create("Beach", Slug.Create("Beach"), "umbrella", 10));

        Assert.Equal(4, await SeedAsync());
        Assert.Equal(0, await SeedAsync()); // running it again adds nothing

        await using var scope = sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var packages = await db.TourPackages.Include(p => p.Images).Include(p => p.ItineraryDays)
            .Where(p => p.Title == "Cox's Bazar Beach Escape" || p.Title == "Sajek Valley Cloud Trip"
                        || p.Title == "Saint Martin's Island Stay" || p.Title == "Sylhet Tea Garden Retreat")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(4, packages.Count);
        Assert.All(packages, p => Assert.Equal(PackageStatus.Published, p.Status));
        Assert.All(packages, p => Assert.Equal(3, p.Images.Count));
        Assert.Equal(2, packages.Count(p => p.PricingMode == PricingMode.FixedDepartures));

        // The photos are real WebP files on disk (in the test's temp folder).
        var storage = scope.ServiceProvider.GetRequiredService<IOptions<LocalDiskStorageOptions>>().Value;
        var keys = await db.FileObjects.Where(f => f.StorageKey.StartsWith("images/demo/")).Select(f => f.StorageKey)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(12, keys.Count);
        foreach (var key in keys)
        {
            var bytes = await File.ReadAllBytesAsync(Path.Combine(storage.RootPath, key), TestContext.Current.CancellationToken);
            Assert.Equal("WEBP", System.Text.Encoding.ASCII.GetString(bytes, 8, 4)); // RIFF....WEBP
        }

        // Fixed: the dates are in the future and a family can book the first one.
        var coxsBazar = packages.Single(p => p.Title == "Cox's Bazar Beach Escape");
        var firstDate = await db.Departures.Where(d => d.PackageId == coxsBazar.Id).OrderBy(d => d.StartDate)
            .FirstAsync(TestContext.Current.CancellationToken);
        var booking = await sql.SendCommandAsync(
            BookingTestData.Book(coxsBazar.Slug.Value, firstDate.Id), await sql.NewCustomerAsync());
        Assert.True(booking.IsSuccess, booking.Error.Message);
        Assert.Equal(2 * 12_500 + 9_500 + 1_000, booking.Value.TotalAmount); // 35,500

        // Flexible: priced from its earliest allowed start.
        var sylhet = packages.Single(p => p.Title == "Sylhet Tea Garden Retreat");
        var quote = await sql.SendAsync(new GetBookingQuoteQuery(
            sylhet.Slug.Value, null, BookingTestData.Today.AddDays(5), 3, Adults: 2));
        Assert.True(quote.IsSuccess, quote.Error.Message);
        Assert.Equal(2 * (7_500 + 2_800), quote.Value.Total); // 2 nights base + 1 extra night, × 2 adults = 20,600
    }
}
