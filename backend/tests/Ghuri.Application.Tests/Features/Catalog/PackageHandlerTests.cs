using Ghuri.Application.Common;
using Ghuri.Application.Features.Catalog.Commands.CreatePackage;
using Ghuri.Application.Features.Catalog.Commands.PublishPackage;
using Ghuri.Application.Features.Catalog.Commands.UpdatePackage;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Tests.Features.Catalog;

public class PackageHandlerTests
{
    private readonly FakeTourPackageRepository _packages = new();
    private readonly FakeDestinationRepository _destinations = new();
    private readonly FakeCategoryRepository _categories = new();
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
        IsFeatured: false, SeoTitle: null, SeoDescription: null,
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
        c.Exclusions, c.TermsAndPolicy, c.MinAge, c.IsFeatured, c.SeoTitle, c.SeoDescription, c.PricingMode,
        c.DurationDays, c.DurationNights, c.MinNights, c.MaxNights, c.BasePrice, c.ExtraNightPrice, c.MinLeadDays);

    private Task<Result<Guid>> CreateAsync(CreatePackageCommand command) =>
        new CreatePackageHandler(_packages, _destinations, _categories)
            .Handle(command, TestContext.Current.CancellationToken).AsTask();

    private Task<Result> UpdateAsync(UpdatePackageCommand command) =>
        new UpdatePackageHandler(_packages, _destinations, _categories)
            .Handle(command, TestContext.Current.CancellationToken).AsTask();

    private Task<Result> PublishAsync(Guid id) =>
        new PublishPackageHandler(_packages, TimeProvider.System)
            .Handle(new PublishPackageCommand(id), TestContext.Current.CancellationToken).AsTask();

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
        var package = _packages.Packages.Single();
        package.SetImages([Guid.NewGuid()]);
        package.SetItinerary([new("Arrive", "Check in"), new("Beach", "Swim"), new("Leave", "Bus home")]);
        await PublishAsync(id);

        var result = await UpdateAsync(ToUpdate(id, FlexibleCommand()));

        Assert.Equal("package_pricing_mode_locked", result.Error.Code);
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
