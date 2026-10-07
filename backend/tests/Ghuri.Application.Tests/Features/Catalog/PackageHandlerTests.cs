using Ghuri.Application.Common;
using Ghuri.Application.Features.Catalog.Commands.CreatePackage;
using Ghuri.Application.Features.Catalog.Commands.PublishPackage;
using Ghuri.Application.Features.Catalog.Commands.UpdatePackage;
using Ghuri.Application.Tests.Features.Identity;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Tests.Features.Catalog;

public class PackageHandlerTests
{
    private readonly FakeTourPackageRepository _packages = new();
    private readonly FakeDestinationRepository _destinations = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeDepartureRepository _departures = new();
    private readonly FakeClock _clock = new(new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc));
    private readonly Guid _coxsBazar;
    private readonly Guid _beach = Guid.NewGuid();

    public PackageHandlerTests()
    {
        var destination = Destination.Create(19, "Cox's Bazar", Slug.Create("Cox's Bazar"));
        _destinations.Add(destination);
        _coxsBazar = destination.Id;
        _categories.CategoryIds.Add(_beach);
    }

    private CreatePackageCommand FixedCommand(string title = "Cox's Bazar 3 Days") => new(
        _coxsBazar, title, Slug: null, Summary: "Sea and sand.", Description: null, TourType.Group,
        CategoryIds: [_beach], Inclusions: ["Hotel"], Exclusions: [], TermsAndPolicy: null, MinAge: null,
        IsFeatured: false,
        PricingMode.FixedDepartures, DurationDays: 3, DurationNights: 2,
        MinNights: null, MaxNights: null, BasePrice: null, ExtraNightPrice: null, MinLeadDays: null);

    private CreatePackageCommand FlexibleCommand() => FixedCommand("Sea Pearl Stay") with
    {
        PricingMode = PricingMode.FlexibleStay,
        DurationDays = null,
        DurationNights = null,
        MinNights = 2,
        MaxNights = 7,
        BasePrice = 8000,
        ExtraNightPrice = 3000,
        MinLeadDays = 3
    };

    private static UpdatePackageCommand ToUpdate(Guid id, CreatePackageCommand c) => new(
        id, c.DestinationId, c.Title, c.Slug, c.Summary, c.Description, c.TourType, c.CategoryIds, c.Inclusions,
        c.Exclusions, c.TermsAndPolicy, c.MinAge, c.IsFeatured, c.PricingMode,
        c.DurationDays, c.DurationNights, c.MinNights, c.MaxNights, c.BasePrice, c.ExtraNightPrice, c.MinLeadDays);

    private Task<Result<Guid>> CreateAsync(CreatePackageCommand command) =>
        new CreatePackageHandler(_packages, _destinations, _categories)
            .Handle(command, TestContext.Current.CancellationToken).AsTask();

    private Task<Result> UpdateAsync(UpdatePackageCommand command) =>
        new UpdatePackageHandler(_packages, _destinations, _categories, _departures)
            .Handle(command, TestContext.Current.CancellationToken).AsTask();

    private Task<Result> PublishAsync(Guid id) =>
        new PublishPackageHandler(_packages, _departures, _clock)
            .Handle(new PublishPackageCommand(id), TestContext.Current.CancellationToken).AsTask();

    /// <summary>Photo + 3-day itinerary + an open departure next month: everything a fixed package needs to publish.</summary>
    private TourPackage MakeReady(TourPackage package)
    {
        package.SetImages([Guid.NewGuid()]);
        package.SetItinerary([new("Arrive", "Check in"), new("Beach", "Swim"), new("Leave", "Bus home")]);
        _departures.Add(Departure.Create(package.Id, new DateOnly(2026, 11, 1), 3, 12_000, 9_000, 0, null, 20, 2));
        return package;
    }

    [Fact]
    public async Task Create_Fixed_AddsADraftWithACodeAndCategories()
    {
        var result = await CreateAsync(FixedCommand());

        Assert.True(result.IsSuccess);
        var package = Assert.Single(_packages.Packages);
        Assert.Equal(result.Value, package.Id);
        Assert.Equal("PKG1001", package.PackageCode);
        Assert.Equal(PackageStatus.Draft, package.Status);
        Assert.Equal("cox-s-bazar-3-days", package.Slug.Value);
        Assert.Equal([_beach], package.Categories.Select(c => c.CategoryId));
    }

    [Fact]
    public async Task Create_Flexible_TakesItsDurationFromTheShortestStay()
    {
        await CreateAsync(FlexibleCommand());

        var package = Assert.Single(_packages.Packages);
        Assert.Equal(PricingMode.FlexibleStay, package.PricingMode);
        Assert.Equal(3, package.DurationDays);
        Assert.Equal(8000, package.PriceFrom);
    }

    [Fact]
    public async Task Create_UnknownDestination_IsRefused_AndAddsNothing()
    {
        var result = await CreateAsync(FixedCommand() with { DestinationId = Guid.NewGuid() });

        Assert.Equal("destination_not_found", result.Error.Code);
        Assert.Empty(_packages.Packages);
    }

    [Fact]
    public async Task Create_UnknownCategory_IsRefused()
    {
        var result = await CreateAsync(FixedCommand() with { CategoryIds = [_beach, Guid.NewGuid()] });

        Assert.Equal("category_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Create_SameTitleTwice_TheSecondSlugIsTaken()
    {
        await CreateAsync(FixedCommand());

        var result = await CreateAsync(FixedCommand());

        Assert.Equal("package_slug_taken", result.Error.Code);
    }

    [Fact]
    public async Task Update_ChangingTheModeOfAPublishedPackage_IsRefused()
    {
        var id = (await CreateAsync(FixedCommand())).Value;
        MakeReady(_packages.Packages.Single());
        Assert.True((await PublishAsync(id)).IsSuccess);

        var result = await UpdateAsync(ToUpdate(id, FlexibleCommand()));

        Assert.Equal("package_pricing_mode_locked", result.Error.Code);
    }

    [Fact]
    public async Task Publish_FixedWithoutDepartures_SaysADepartureIsMissing()
    {
        var id = (await CreateAsync(FixedCommand())).Value;
        var package = _packages.Packages.Single();
        package.SetImages([Guid.NewGuid()]);
        package.SetItinerary([new("Arrive", "Check in"), new("Beach", "Swim"), new("Leave", "Bus home")]);

        var result = await PublishAsync(id);

        Assert.Equal("package_not_publishable", result.Error.Code);
        Assert.Contains("departure", result.Error.Message);
    }

    [Fact]
    public async Task Update_DraftWithDepartures_CannotSwitchToFlexible()
    {
        var id = (await CreateAsync(FixedCommand())).Value;
        MakeReady(_packages.Packages.Single());

        var result = await UpdateAsync(ToUpdate(id, FlexibleCommand()));

        Assert.Equal("package_has_departures", result.Error.Code);
    }

    [Fact]
    public async Task Update_NewDuration_MovesTheDeparturesEndDates()
    {
        var id = (await CreateAsync(FixedCommand())).Value;
        MakeReady(_packages.Packages.Single());

        var fourDays = FixedCommand() with { DurationDays = 4, DurationNights = 3 };
        var result = await UpdateAsync(ToUpdate(id, fourDays));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 4), _departures.Departures.Single().EndDate); // Nov 1-4
    }

    [Fact]
    public async Task Update_Draft_CanSwitchToFlexible()
    {
        var id = (await CreateAsync(FixedCommand())).Value;

        var result = await UpdateAsync(ToUpdate(id, FlexibleCommand()));

        Assert.True(result.IsSuccess);
        Assert.Equal(PricingMode.FlexibleStay, _packages.Packages.Single().PricingMode);
    }

    [Fact]
    public async Task Publish_IncompletePackage_ListsEveryProblem_AndStaysDraft()
    {
        var id = (await CreateAsync(FixedCommand())).Value;

        var result = await PublishAsync(id);

        Assert.Equal("package_not_publishable", result.Error.Code);
        Assert.Contains("photo", result.Error.Message);
        Assert.Contains("itinerary", result.Error.Message);
        Assert.Equal(PackageStatus.Draft, _packages.Packages.Single().Status);
    }

    [Fact]
    public async Task Publish_UnknownPackage_IsNotFound()
    {
        var result = await PublishAsync(Guid.NewGuid());

        Assert.Equal("package_not_found", result.Error.Code);
    }
}
