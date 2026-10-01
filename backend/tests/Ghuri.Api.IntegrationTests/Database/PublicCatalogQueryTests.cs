using Ghuri.Application.Features.Booking.Queries.GetBookingQuote;
using Ghuri.Application.Features.Catalog.Queries.GetDepartureAvailability;
using Ghuri.Application.Features.Catalog.Queries.GetPackageDetails;
using Ghuri.Application.Features.Catalog.Queries.SearchPackages;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// The public queries (Day 6) against real SQL Server: they lean on SQL
/// translation (live prices, slug comparisons, subqueries) that only a real
/// database can prove. Each test builds its own destination and searches
/// only inside it, so tests never see each other's packages.
/// </summary>
public class PublicCatalogQueryTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    // The same "today" the app uses: Bangladesh, UTC+6.
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6));
    private static readonly DateTime Now = DateTime.UtcNow;

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private async Task<Destination> NewDestinationAsync()
    {
        var unique = Unique();
        var destination = Destination.Create(sql.CountryId, $"Place {unique}", Slug.Create($"place-{unique}"));
        await sql.SaveAsync(destination);
        return destination;
    }

    private static TourPackage NewPackage(Destination destination, string title, PackagePricing pricing, bool featured = false) =>
        TourPackage.Create(
            $"T{Unique()}",
            new TourPackageDetails(destination.Id, title, Slug.Create($"{title}-{Unique()}"), "Summary", null,
                TourType.Group, ["Hotel"], [], null, null, featured, null, null),
            pricing);

    private static Departure Dep(
        TourPackage package, int daysFromToday, decimal adultPrice, short seats = 20, byte cutoffDays = 2, bool closed = false)
    {
        var departure = Departure.Create(
            package.Id, Today.AddDays(daysFromToday), package.DurationDays, adultPrice, 9_000, 0, 2_500, seats, cutoffDays);
        if (closed)
            departure.Close();
        return departure;
    }

    /// <summary>Gives the package a cover and an itinerary, publishes it, and saves it with its departures.</summary>
    private async Task<TourPackage> PublishAsync(TourPackage package, params Departure[] departures)
    {
        var cover = FileObject.Create($"test/{Unique()}.webp", "cover.jpg", "image/webp", 1234, new string('a', 64), true, Now);
        package.SetImages([cover.Id]);
        package.SetItinerary(Enumerable.Range(1, package.DurationDays)
            .Select(n => new ItineraryDayDetails($"Day {n}", "Plan")).ToList());
        package.Publish(Now, hasOpenDeparture: true);
        await sql.SaveAsync([cover, package, .. departures]);
        return package;
    }

    private async Task<IReadOnlyList<PackageCardDto>> SearchAsync(Destination destination, PackageSort sort = PackageSort.PriceLow,
        PricingMode? mode = null, decimal? maxPrice = null, string? category = null)
    {
        var result = await sql.SendAsync(new SearchPackagesQuery(null, destination.Slug.Value, category, null, maxPrice, mode, sort));
        Assert.True(result.IsSuccess, result.Error.Message);
        return result.Value.Items;
    }

    // ---------- SearchPackages ----------

    [Fact]
    public async Task Search_ShowsOnlyPublished_WithLiveFromPrices_AndNoDatesLast()
    {
        var place = await NewDestinationAsync();

        var fixedTour = NewPackage(place, "Fixed", PackagePricing.FixedDepartures(3, 2));
        await PublishAsync(fixedTour,
            Dep(fixedTour, -5, 5_000),                // already left - ignored
            Dep(fixedTour, 1, 6_000, cutoffDays: 2),  // booking already closed - ignored
            Dep(fixedTour, 20, 7_000, closed: true),  // closed - ignored
            Dep(fixedTour, 30, 12_000),               // the cheapest BOOKABLE date
            Dep(fixedTour, 40, 15_000));

        var flexible = NewPackage(place, "Flexible", PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3));
        await PublishAsync(flexible);

        var noDates = NewPackage(place, "NoDates", PackagePricing.FixedDepartures(3, 2));
        await PublishAsync(noDates, Dep(noDates, -10, 4_000));

        var draft = NewPackage(place, "Draft", PackagePricing.FixedDepartures(3, 2));
        await sql.SaveAsync(draft); // never published

        var cards = await SearchAsync(place);

        Assert.Equal(["Flexible", "Fixed", "NoDates"], cards.Select(c => c.Title));
        Assert.Equal([8_000m, 12_000m, null], cards.Select(c => c.PriceFrom));
        Assert.All(cards, c => Assert.NotNull(c.CoverImageUrl));
    }

    [Fact]
    public async Task Search_FiltersByModeCategoryAndPrice()
    {
        var place = await NewDestinationAsync();
        var beach = Category.Create($"Beach {Unique()}", Slug.Create($"beach-{Unique()}"));
        await sql.SaveAsync(beach);

        var fixedTour = NewPackage(place, "Fixed", PackagePricing.FixedDepartures(3, 2));
        fixedTour.SetCategories([beach.Id]);
        await PublishAsync(fixedTour, Dep(fixedTour, 30, 12_000));
        var flexible = NewPackage(place, "Flexible", PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3));
        await PublishAsync(flexible);

        Assert.Equal(["Flexible"], (await SearchAsync(place, mode: PricingMode.FlexibleStay)).Select(c => c.Title));
        Assert.Equal(["Fixed"], (await SearchAsync(place, category: beach.Slug.Value)).Select(c => c.Title));
        Assert.Equal(["Flexible"], (await SearchAsync(place, maxPrice: 10_000)).Select(c => c.Title));
    }

    [Fact]
    public async Task Search_Recommended_PutsFeaturedFirst()
    {
        var place = await NewDestinationAsync();
        await PublishAsync(NewPackage(place, "Plain", PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3)));
        await PublishAsync(NewPackage(place, "Featured", PackagePricing.FlexibleStay(2, 7, 9_000, 3_000, 3), featured: true));

        Assert.Equal("Featured", (await SearchAsync(place, PackageSort.Recommended))[0].Title);
    }

    // ---------- GetPackageDetails ----------

    [Fact]
    public async Task Details_PublishedPackage_HasEverythingThePageNeeds()
    {
        var place = await NewDestinationAsync();
        var tour = await PublishAsync(NewPackage(place, "Details", PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3)));

        var result = await sql.SendAsync(new GetPackageDetailsQuery(tour.Slug.Value));

        Assert.True(result.IsSuccess, result.Error.Message);
        var details = result.Value;
        Assert.Equal(place.Name, details.DestinationName);
        Assert.Equal("Bangladesh", details.CountryName);
        Assert.Single(details.ImageUrls);
        Assert.Equal(3, details.Itinerary.Count);
        Assert.Equal(Today.AddDays(3), details.EarliestStartDate); // 3 lead days
        Assert.Equal("Details", details.SeoTitle);                 // falls back to the title
    }

    [Fact]
    public async Task Details_DraftPackage_IsNotFound()
    {
        var place = await NewDestinationAsync();
        var draft = NewPackage(place, "Draft", PackagePricing.FixedDepartures(3, 2));
        await sql.SaveAsync(draft);

        Assert.Equal("package_not_found", (await sql.SendAsync(new GetPackageDetailsQuery(draft.Slug.Value))).Error.Code);
    }

    // ---------- GetDepartureAvailability ----------

    [Fact]
    public async Task Availability_OnlyBookableDates_InOrder_SoldOutIncluded()
    {
        var place = await NewDestinationAsync();
        var tour = NewPackage(place, "Avail", PackagePricing.FixedDepartures(3, 2));
        var soldOut = Dep(tour, 35, 11_000, seats: 1);
        await PublishAsync(tour,
            Dep(tour, -5, 5_000), Dep(tour, 1, 6_000, cutoffDays: 2), Dep(tour, 20, 7_000, closed: true),
            Dep(tour, 40, 15_000), Dep(tour, 30, 12_000), soldOut);
        await using (var scope = sql.Services.CreateAsyncScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<IDepartureRepository>()
                .TryReserveSeatsAsync(soldOut.Id, 1, TestContext.Current.CancellationToken));

        var result = await sql.SendAsync(new GetDepartureAvailabilityQuery(tour.Slug.Value));

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal([30, 35, 40], result.Value.Select(d => d.StartDate.DayNumber - Today.DayNumber));
        Assert.Equal(0, result.Value.Single(d => d.Id == soldOut.Id).SeatsLeft);
    }

    // ---------- GetBookingQuote ----------

    [Fact]
    public async Task Quote_FixedDeparture_PricesEachTravellerType()
    {
        var place = await NewDestinationAsync();
        var tour = NewPackage(place, "QuoteFixed", PackagePricing.FixedDepartures(3, 2));
        var departure = Dep(tour, 30, 12_000);
        await PublishAsync(tour, departure);

        // 2 adults × 12,000 + 1 child × 9,000 + 1 single room × 2,500 = 35,500
        var result = await sql.SendAsync(new GetBookingQuoteQuery(tour.Slug.Value, departure.Id, null, null, 2, 1, 0, 1));

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal(35_500, result.Value.Total);
        Assert.Equal(departure.EndDate, result.Value.EndDate);
    }

    [Fact]
    public async Task Quote_FixedDeparture_RefusesClosedBookingAndTooFewSeats()
    {
        var place = await NewDestinationAsync();
        var tour = NewPackage(place, "QuoteRefuse", PackagePricing.FixedDepartures(3, 2));
        var tooLate = Dep(tour, 1, 6_000, cutoffDays: 2);
        var small = Dep(tour, 30, 12_000, seats: 2);
        await PublishAsync(tour, tooLate, small);

        Assert.Equal("booking_closed",
            (await sql.SendAsync(new GetBookingQuoteQuery(tour.Slug.Value, tooLate.Id, null, null, 1))).Error.Code);
        Assert.Equal("not_enough_seats",
            (await sql.SendAsync(new GetBookingQuoteQuery(tour.Slug.Value, small.Id, null, null, 3))).Error.Code);
    }

    [Fact]
    public async Task Quote_FlexibleStay_PricesTheNights_AndChecksLeadDays()
    {
        var place = await NewDestinationAsync();
        var tour = await PublishAsync(NewPackage(place, "QuoteFlex", PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3)));
        var start = Today.AddDays(5);

        // 3 nights = 8,000 + 1 × 3,000 = 11,000 per person; 2 adults = 22,000
        var result = await sql.SendAsync(new GetBookingQuoteQuery(tour.Slug.Value, null, start, 3, 2));

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal(22_000, result.Value.Total);
        Assert.Equal(start.AddDays(3), result.Value.EndDate); // check-out day

        Assert.Equal("start_date_too_soon",
            (await sql.SendAsync(new GetBookingQuoteQuery(tour.Slug.Value, null, Today.AddDays(1), 3, 2))).Error.Code);
        Assert.Equal("nights_out_of_range",
            (await sql.SendAsync(new GetBookingQuoteQuery(tour.Slug.Value, null, start, 8, 2))).Error.Code);
    }

    // ---------- Over HTTP: URL binding (enum names, dates) and status codes ----------

    [Fact]
    public async Task Http_SearchAndQuote_BindEnumNamesAndDates_WithoutLogin()
    {
        var place = await NewDestinationAsync();
        var tour = await PublishAsync(NewPackage(place, "Http", PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3)));
        var client = sql.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var search = await client.GetAsync($"/api/v1/packages?destination={place.Slug.Value}&mode=FlexibleStay&sort=PriceLow", ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, search.StatusCode);
        Assert.Contains(tour.Slug.Value, await search.Content.ReadAsStringAsync(ct));

        var start = Today.AddDays(5).ToString("yyyy-MM-dd");
        var quote = await client.GetAsync($"/api/v1/packages/{tour.Slug.Value}/quote?startDate={start}&nights=3&adults=2", ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, quote.StatusCode);
        Assert.Contains("\"total\":22000", await quote.Content.ReadAsStringAsync(ct));
    }

    [Fact]
    public async Task Http_UnknownPackage_Is404_AndBadTravellers_Is400()
    {
        var place = await NewDestinationAsync();
        var tour = await PublishAsync(NewPackage(place, "Http400", PackagePricing.FlexibleStay(2, 7, 8_000, 3_000, 3)));
        var client = sql.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/packages/no-such-package", ct)).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest,
            (await client.GetAsync($"/api/v1/packages/{tour.Slug.Value}/quote?nights=3&adults=0", ct)).StatusCode);
    }
}
