using Ghuri.Application.Common;
using Ghuri.Application.Features.Catalog.Commands.CloseDeparture;
using Ghuri.Application.Features.Catalog.Commands.CreateDeparture;
using Ghuri.Application.Features.Catalog.Commands.UpdateDeparture;
using Ghuri.Application.Tests.Features.Identity;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Tests.Features.Catalog;

public class DepartureHandlerTests
{
    // 1 Oct 2026, 20:00 UTC = 2 Oct 02:00 in Dhaka: "today" is ALREADY the 2nd there.
    private static readonly DateTime Now = new(2026, 10, 1, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 2);

    private readonly FakeTourPackageRepository _packages = new();
    private readonly FakeDepartureRepository _departures = new();
    private readonly FakeClock _clock = new(Now);
    private readonly TourPackage _fixed;

    public DepartureHandlerTests()
    {
        _fixed = NewPackage("Beach Escape", PackagePricing.FixedDepartures(3, 2));
        _packages.Add(_fixed);
    }

    private static TourPackage NewPackage(string title, PackagePricing pricing) => TourPackage.Create(
        "PKG1001",
        new TourPackageDetails(Guid.NewGuid(), title, Slug.Create(title), "Summary", null, TourType.Group,
            [], [], null, null, false, null, null),
        pricing);

    private static CreateDepartureCommand Command(Guid packageId, DateOnly start, decimal adultPrice = 12_000) =>
        new(packageId, start, adultPrice, ChildPrice: 9_000, InfantPrice: 0, SingleSupplement: null, TotalSeats: 20, BookingCutoffDays: 2);

    private Task<Result<Guid>> CreateAsync(CreateDepartureCommand command) =>
        new CreateDepartureHandler(_packages, _departures, _clock).Handle(command, TestContext.Current.CancellationToken).AsTask();

    private Task<Result> UpdateAsync(UpdateDepartureCommand command) =>
        new UpdateDepartureHandler(_packages, _departures, _clock).Handle(command, TestContext.Current.CancellationToken).AsTask();

    private Task<Result> CloseAsync(Guid id) =>
        new CloseDepartureHandler(_packages, _departures, _clock)
            .Handle(new CloseDepartureCommand(id), TestContext.Current.CancellationToken).AsTask();

    [Fact]
    public async Task Create_AddsAnOpenDeparture_EndingAfterThePackageDuration()
    {
        var result = await CreateAsync(Command(_fixed.Id, new DateOnly(2026, 12, 20)));

        Assert.True(result.IsSuccess);
        var departure = Assert.Single(_departures.Departures);
        Assert.Equal(new DateOnly(2026, 12, 22), departure.EndDate);
        Assert.Equal(DepartureStatus.Open, departure.Status);
    }

    [Fact]
    public async Task Create_OnAFlexiblePackage_IsRefused()
    {
        var flexible = NewPackage("Sea Pearl Stay", PackagePricing.FlexibleStay(2, 7, 8000, 3000, 3));
        _packages.Add(flexible);

        var result = await CreateAsync(Command(flexible.Id, new DateOnly(2026, 12, 20)));

        Assert.Equal("departure_needs_fixed_package", result.Error.Code);
    }

    [Fact]
    public async Task Create_ForYesterdayInDhaka_IsRefused_EvenThoughItIsStillTodayInUtc()
    {
        // 1 Oct is "today" by the UTC clock, but in Dhaka it's already 2 Oct.
        var result = await CreateAsync(Command(_fixed.Id, new DateOnly(2026, 10, 1)));

        Assert.Equal("departure_date_in_past", result.Error.Code);
    }

    [Fact]
    public async Task Create_ForTodayInDhaka_IsAllowed() =>
        Assert.True((await CreateAsync(Command(_fixed.Id, Today))).IsSuccess);

    [Fact]
    public async Task Create_SecondDepartureOnTheSameDay_IsRefused()
    {
        await CreateAsync(Command(_fixed.Id, new DateOnly(2026, 12, 20)));

        var result = await CreateAsync(Command(_fixed.Id, new DateOnly(2026, 12, 20)));

        Assert.Equal("departure_date_taken", result.Error.Code);
    }

    [Fact]
    public async Task PriceFrom_FollowsTheCheapestOpenDeparture()
    {
        await CreateAsync(Command(_fixed.Id, new DateOnly(2026, 12, 20), adultPrice: 15_000));
        Assert.Equal(15_000, _fixed.PriceFrom);

        var cheaper = (await CreateAsync(Command(_fixed.Id, new DateOnly(2027, 1, 10), adultPrice: 12_500))).Value;
        Assert.Equal(12_500, _fixed.PriceFrom);

        // Closing the cheapest date raises "from" back to the other one...
        await CloseAsync(cheaper);
        Assert.Equal(15_000, _fixed.PriceFrom);

        // ...and closing the last one leaves no price (0 = "no departures yet").
        await CloseAsync(_departures.Departures.Single(d => d.Status == DepartureStatus.Open).Id);
        Assert.Equal(0, _fixed.PriceFrom);
    }

    [Fact]
    public async Task Update_NewPrice_UpdatesPriceFrom()
    {
        var id = (await CreateAsync(Command(_fixed.Id, new DateOnly(2026, 12, 20), adultPrice: 15_000))).Value;

        var result = await UpdateAsync(new UpdateDepartureCommand(id, new DateOnly(2026, 12, 20), 13_000, 9_000, 0, null, 20, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(13_000, _fixed.PriceFrom);
    }

    [Fact]
    public async Task Update_MovingOntoAnotherDeparturesDate_IsRefused()
    {
        await CreateAsync(Command(_fixed.Id, new DateOnly(2026, 12, 20)));
        var second = (await CreateAsync(Command(_fixed.Id, new DateOnly(2026, 12, 27)))).Value;

        var result = await UpdateAsync(new UpdateDepartureCommand(second, new DateOnly(2026, 12, 20), 12_000, 9_000, 0, null, 20, 2));

        Assert.Equal("departure_date_taken", result.Error.Code);
    }

    [Fact]
    public async Task Close_Twice_IsRefused()
    {
        var id = (await CreateAsync(Command(_fixed.Id, new DateOnly(2026, 12, 20)))).Value;
        await CloseAsync(id);

        Assert.Equal("departure_not_open", (await CloseAsync(id)).Error.Code);
    }
}
