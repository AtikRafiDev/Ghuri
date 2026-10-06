using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ghuri.Infrastructure.Persistence.Seed;

/// <summary>
/// DEVELOPMENT ONLY: four PUBLISHED, ready-to-book packages - two with fixed
/// departures, two flexible stays - each with three generated photos, an
/// itinerary, inclusions and categories. Lets the checkout and payment be
/// tried without first building packages by hand in the admin.
/// </summary>
/// <remarks>
/// <para>
/// Part of "dotnet run --project src/Ghuri.Api -- seed-demo" (after the demo
/// destinations and categories it needs). Built through the real domain
/// methods - SetImages, SetItinerary, Publish - so every publish rule is
/// checked exactly as in the admin.
/// </para>
/// <para>
/// Safe to run again: a package whose slug exists is skipped. Departure dates
/// are counted from today (10, 24, 38 days ahead...), so they are in the
/// future when seeded - once they pass, add new dates in the admin, or delete
/// the package there and seed again.
/// </para>
/// </remarks>
internal sealed class DemoPackageSeeder(
    AppDbContext db,
    ITourPackageRepository packages,
    DemoPhotoWriter photos,
    TimeProvider clock,
    ILogger<DemoPackageSeeder> logger)
{
    private sealed record DemoDay(string Title, string Description, string? Meals = null, string? Accommodation = null);

    private sealed record DemoDeparture(int DaysFromToday, decimal Adult, decimal Child, decimal Infant, decimal? SingleSupplement, short Seats);

    private sealed record DemoPackage(
        string Destination,
        string Title,
        string Summary,
        string Description,
        TourType TourType,
        PackagePricing Pricing,
        string[] Categories,
        string[] Inclusions,
        string[] Exclusions,
        DemoDay[] Days,
        DemoDeparture[] Departures,
        string SkyTop,
        string SkyBottom,
        bool IsFeatured = false);

    private static readonly DemoPackage[] Packages =
    [
        // ---------- Fixed departures ----------
        new(
            Destination: "Cox's Bazar",
            Title: "Cox's Bazar Beach Escape",
            Summary: "Three days on the world's longest sea beach - Himchari, Inani and a sunset drive along Marine Drive.",
            Description: "Leave Dhaka at night by AC bus and wake up by the Bay of Bengal. Two nights in a sea-view hotel, a guided day "
                + "along Marine Drive to Himchari waterfall and Inani's coral beach, and a free evening for Burmese market shopping.",
            TourType: TourType.Group,
            Pricing: PackagePricing.FixedDepartures(3, 2),
            Categories: ["Beach", "Family", "Weekend Getaway"],
            Inclusions: ["AC bus Dhaka - Cox's Bazar - Dhaka", "2 nights in a sea-view hotel", "Daily breakfast", "Himchari and Inani day tour", "Tour guide"],
            Exclusions: ["Lunch and dinner", "Personal expenses", "Water sports"],
            Days:
            [
                new("Arrival and beach time", "Arrive in the morning, check in, and spend the afternoon on Laboni beach. Sunset walk on Sugandha point.", "B", "Sea-view hotel"),
                new("Marine Drive, Himchari and Inani", "Drive the coast road past palm-lined beaches to Himchari waterfall and Inani's coral stones.", "B", "Sea-view hotel"),
                new("Burmese market and departure", "Morning free, Burmese market for souvenirs, then the overnight bus back to Dhaka.", "B"),
            ],
            Departures:
            [
                new(10, 12_500, 9_500, 1_000, 3_000, 20),
                new(24, 13_000, 9_800, 1_000, 3_000, 20),
                new(38, 12_500, 9_500, 1_000, 3_000, 4), // nearly full: shows "4 seats left"
            ],
            SkyTop: "#0EA5E9", SkyBottom: "#FDE68A", IsFeatured: true),
        new(
            Destination: "Sajek Valley",
            Title: "Sajek Valley Cloud Trip",
            Summary: "Wake up above a sea of clouds in the 'queen of hills', with Konglak Para at sunset and Khagrachari's caves.",
            Description: "By AC bus to Khagrachari, then up the winding hill road by open jeep to Sajek. A night in a hilltop cottage, "
                + "sunrise above the clouds, the climb to Konglak Para, and Alutila cave and Risang waterfall on the way back.",
            TourType: TourType.Group,
            Pricing: PackagePricing.FixedDepartures(3, 2),
            Categories: ["Hill and Mountain", "Adventure"],
            Inclusions: ["AC bus Dhaka - Khagrachari - Dhaka", "Jeep to Sajek and back", "2 nights in hill cottages", "All meals in Sajek", "Tour guide"],
            Exclusions: ["Personal expenses", "Entry tickets"],
            Days:
            [
                new("Khagrachari to Sajek", "Breakfast in Khagrachari, jeep convoy to Sajek, Konglak Para at sunset.", "B,L,D", "Hill cottage"),
                new("Above the clouds", "Sunrise over the clouds from the helipad, a slow day in the valley, campfire at night.", "B,L,D", "Hill cottage"),
                new("Alutila and Risang", "Back down to Khagrachari: Alutila cave by torchlight and Risang waterfall, then the bus home.", "B,L"),
            ],
            Departures:
            [
                new(14, 8_900, 7_000, 0, null, 25),
                new(28, 9_200, 7_200, 0, null, 25),
            ],
            SkyTop: "#6366F1", SkyBottom: "#A7F3D0"),

        // ---------- Flexible stays ----------
        new(
            Destination: "Saint Martin's Island",
            Title: "Saint Martin's Island Stay",
            Summary: "Your own dates on Bangladesh's coral island - turquoise water, coconut palms and seafood on the beach.",
            Description: "Choose your check-in day and stay 2 to 5 nights in a beach resort. Ship transfer from Teknaf, a cycle ride "
                + "round the island and a boat to Chhera Dwip are included. Hotel rooms are confirmed within 24 hours.",
            TourType: TourType.Private,
            Pricing: PackagePricing.FlexibleStay(2, 5, 9_000, 3_500, 3),
            Categories: ["Beach", "Honeymoon"],
            Inclusions: ["Ship Teknaf - Saint Martin - Teknaf", "Beach resort room", "Daily breakfast", "Boat to Chhera Dwip"],
            Exclusions: ["Travel to Teknaf", "Lunch and dinner", "Personal expenses"],
            Days:
            [
                new("Ship to the island", "Morning ship from Teknaf across the Naf river mouth; check in and watch the sunset on the west beach.", "B", "Beach resort"),
                new("Chhera Dwip", "Boat to Chhera Dwip, the island's southern tip, then a free afternoon in the water.", "B", "Beach resort"),
                new("Island at your pace", "Cycle round the island, or simply stay on the beach. Extra nights continue the same way.", "B", "Beach resort"),
            ],
            Departures: [],
            SkyTop: "#06B6D4", SkyBottom: "#FBCFE8", IsFeatured: true),
        new(
            Destination: "Sylhet",
            Title: "Sylhet Tea Garden Retreat",
            Summary: "A tea-estate resort on your dates - Ratargul swamp forest, Jaflong and the clear water of Bholaganj.",
            Description: "Stay 2 to 4 nights in a resort among Sylhet's tea gardens. Day trips by private car to the Ratargul swamp "
                + "forest and Jaflong are included; Bholaganj Sada Pathor can be added on a longer stay.",
            TourType: TourType.Private,
            Pricing: PackagePricing.FlexibleStay(2, 4, 7_500, 2_800, 2),
            Categories: ["Nature and Wildlife", "Family"],
            Inclusions: ["Tea-garden resort room", "Daily breakfast", "Private car for the day trips", "Boat in Ratargul"],
            Exclusions: ["Travel to Sylhet", "Lunch and dinner", "Personal expenses"],
            Days:
            [
                new("Arrival among the tea gardens", "Check in, walk the tea estate in the afternoon light.", "B", "Tea-garden resort"),
                new("Ratargul and Jaflong", "Boat through the Ratargul swamp forest, then Jaflong's stone-filled river below the Khasi hills.", "B", "Tea-garden resort"),
            ],
            Departures: [],
            SkyTop: "#16A34A", SkyBottom: "#FEF3C7"),
    ];

    /// <summary>Adds every demo package that isn't there yet. Needs the demo destinations and categories first.</summary>
    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var destinations = await db.Destinations.ToDictionaryAsync(d => d.Slug, d => d.Id, cancellationToken);
        var categories = await db.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id, cancellationToken);
        var existing = (await db.TourPackages.Select(p => p.Slug).ToListAsync(cancellationToken)).ToHashSet();
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var today = clock.Today();
        var added = 0;

        foreach (var demo in Packages)
        {
            var slug = Slug.Create(demo.Title);
            if (existing.Contains(slug))
                continue;
            if (!destinations.TryGetValue(Slug.Create(demo.Destination), out var destinationId))
            {
                logger.LogWarning("Demo package {Title} skipped: destination {Destination} not found.", demo.Title, demo.Destination);
                continue;
            }

            var package = TourPackage.Create(
                await packages.NextPackageCodeAsync(cancellationToken),
                new TourPackageDetails(destinationId, demo.Title, slug, demo.Summary, demo.Description, demo.TourType,
                    demo.Inclusions, demo.Exclusions, TermsAndPolicy: null, MinAge: null, demo.IsFeatured,
                    SeoTitle: $"{demo.Title} | Ghuri", SeoDescription: demo.Summary.Length <= 160 ? demo.Summary : demo.Summary[..157] + "..."),
                demo.Pricing);

            package.SetCategories(demo.Categories
                .Select(name => categories.GetValueOrDefault(Slug.Create(name)))
                .Where(id => id != Guid.Empty));
            package.SetImages(await DrawPhotosAsync(demo, nowUtc, cancellationToken));
            package.SetItinerary(demo.Days.Select(d => new ItineraryDayDetails(d.Title, d.Description, d.Meals, d.Accommodation)).ToList());

            var departures = demo.Departures
                .Select(d => Departure.Create(package.Id, today.AddDays(d.DaysFromToday), package.DurationDays,
                    d.Adult, d.Child, d.Infant, d.SingleSupplement, d.Seats, bookingCutoffDays: 2))
                .ToList();
            db.Departures.AddRange(departures);
            if (departures.Count > 0)
                package.SetLowestDeparturePrice(departures.Min(d => d.AdultPrice)); // the "from ৳…" price

            // The admin's own publish rules: cover photo, itinerary, prices, an open date for fixed packages.
            package.Publish(nowUtc, hasOpenDeparture: departures.Count > 0);
            db.TourPackages.Add(package);
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return added;
    }

    /// <summary>Three postcards per package (the first is the cover).</summary>
    private Task<List<Guid>> DrawPhotosAsync(DemoPackage demo, DateTime nowUtc, CancellationToken cancellationToken) =>
        photos.SaveAsync(
            demo.Title,
            [(demo.Title, demo.Destination), (demo.Title, $"{demo.Destination} - day trip"), (demo.Title, $"{demo.Destination} - evening")],
            demo.SkyTop, demo.SkyBottom, nowUtc, cancellationToken);
}
