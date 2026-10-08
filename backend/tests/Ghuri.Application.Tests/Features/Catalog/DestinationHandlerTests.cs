using Ghuri.Application.Features.Catalog;
using Ghuri.Application.Features.Catalog.Commands.CreateDestination;
using Ghuri.Application.Features.Catalog.Commands.UpdateDestination;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Services;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Tests.Features.Catalog;

public class DestinationHandlerTests
{
    private const short Bangladesh = 19;

    private readonly FakeDestinationRepository _destinations = new() { CountryIds = { Bangladesh } };
    private readonly FakeFileObjectRepository _files = new();

    private Guid UploadedPhoto()
    {
        var id = Guid.NewGuid();
        _files.FileIds.Add(id);
        return id;
    }

    private static CreateDestinationCommand CreateCommand(IReadOnlyList<Guid> imageFileIds, string name = "Sylhet", int? sortOrder = 0) =>
        new(Bangladesh, name, Slug: null, Summary: null, imageFileIds, IsFeatured: false, sortOrder);

    private static UpdateDestinationCommand UpdateCommand(Guid id, IReadOnlyList<Guid> imageFileIds, int? sortOrder = 0) =>
        new(id, Bangladesh, "Sylhet", Slug: null, Summary: null, imageFileIds, IsFeatured: false, sortOrder);

    private Destination Existing(string name, int sortOrder)
    {
        var destination = Destination.Create(Bangladesh, name, Slug.Create(name), sortOrder: sortOrder);
        _destinations.Add(destination);
        return destination;
    }

    private Task<Ghuri.Application.Common.Result<Guid>> CreateAsync(CreateDestinationCommand command) =>
        new CreateDestinationHandler(_destinations, _files).Handle(command, TestContext.Current.CancellationToken).AsTask();

    private Task<Ghuri.Application.Common.Result> UpdateAsync(UpdateDestinationCommand command) =>
        new UpdateDestinationHandler(_destinations, _files).Handle(command, TestContext.Current.CancellationToken).AsTask();

    [Fact]
    public async Task Create_WithPhotos_SavesTheGalleryInTheGivenOrder()
    {
        var (first, second) = (UploadedPhoto(), UploadedPhoto());

        var result = await CreateAsync(CreateCommand([second, first]));

        Assert.True(result.IsSuccess);
        var destination = Assert.Single(_destinations.Destinations);
        Assert.Equal(result.Value, destination.Id);
        Assert.Equal([second, first], destination.Images.Select(i => i.FileId));
    }

    [Fact]
    public async Task Create_WithAPhotoThatWasNeverUploaded_IsRefused_AndAddsNothing()
    {
        var result = await CreateAsync(CreateCommand([UploadedPhoto(), Guid.NewGuid()]));

        Assert.Equal("image_not_found", result.Error.Code);
        Assert.Empty(_destinations.Destinations);
    }

    [Fact]
    public async Task Create_WithoutThePhotoList_MeansNoPhotos()
    {
        // JSON without "imageFileIds" binds the list as null, despite its C# type.
        var result = await CreateAsync(CreateCommand(null!));

        Assert.True(result.IsSuccess);
        Assert.Empty(Assert.Single(_destinations.Destinations).Images);
    }

    [Fact]
    public async Task Create_WithATakenSlug_IsRefused()
    {
        _destinations.Add(Destination.Create(Bangladesh, "Sylhet", Slug.Create("Sylhet")));

        var result = await CreateAsync(CreateCommand([]));

        Assert.Equal("destination_slug_taken", result.Error.Code);
        Assert.Single(_destinations.Destinations);
    }

    [Fact]
    public async Task Update_ReplacesTheGallery()
    {
        var (a, b, c) = (UploadedPhoto(), UploadedPhoto(), UploadedPhoto());
        var destination = Destination.Create(Bangladesh, "Sylhet", Slug.Create("Sylhet"));
        destination.SetImages([a, b]);
        _destinations.Add(destination);

        var result = await UpdateAsync(UpdateCommand(destination.Id, [c, a]));

        Assert.True(result.IsSuccess);
        Assert.Equal([c, a], destination.Images.Select(i => i.FileId));
    }

    [Fact]
    public async Task Update_AnUnknownDestination_IsNotFound()
    {
        var result = await UpdateAsync(UpdateCommand(Guid.NewGuid(), []));

        Assert.Equal("destination_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Update_KeepingItsOwnSlug_IsAllowed()
    {
        var destination = Destination.Create(Bangladesh, "Sylhet", Slug.Create("Sylhet"));
        _destinations.Add(destination);

        var result = await UpdateAsync(UpdateCommand(destination.Id, []));

        Assert.True(result.IsSuccess);
    }

    // ---------- Sort order: unique, the others move along (2026-10-08) ----------

    [Fact]
    public async Task Create_OnATakenNumber_MovesThatDestinationAlong()
    {
        var coxsBazar = Existing("Cox's Bazar", 20);
        var sajek = Existing("Sajek Valley", 30);

        var result = await CreateAsync(CreateCommand([], "Goa", sortOrder: 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(20, _destinations.Destinations.Single(d => d.Id == result.Value).SortOrder);
        Assert.Equal((21, 30), (coxsBazar.SortOrder, sajek.SortOrder)); // 21 was free, so Sajek stays
    }

    [Fact]
    public async Task Create_WithoutANumber_GoesAfterTheLast()
    {
        Existing("Cox's Bazar", 20);
        Existing("Sajek Valley", 90);

        var result = await CreateAsync(CreateCommand([], "Goa", sortOrder: null));

        Assert.Equal(90 + DisplayOrder.Step, _destinations.Destinations.Single(d => d.Id == result.Value).SortOrder);
    }

    [Fact]
    public async Task Create_ThatIsRefused_MovesNothing()
    {
        var sylhet = Existing("Sylhet", 20);

        var result = await CreateAsync(CreateCommand([], "Sylhet", sortOrder: 20)); // slug "sylhet" is taken

        Assert.Equal("destination_slug_taken", result.Error.Code);
        Assert.Equal(20, sylhet.SortOrder);
    }

    [Fact]
    public async Task Update_OntoATakenNumber_MovesTheOthersOnlyUpToTheFirstGap()
    {
        var a = Existing("Cox's Bazar", 2);
        var b = Existing("Goa", 3);
        var c = Existing("Bandarban", 10);
        var sylhet = Existing("Sylhet", 40);

        var result = await UpdateAsync(UpdateCommand(sylhet.Id, [], sortOrder: 2));

        Assert.True(result.IsSuccess);
        Assert.Equal((2, 3, 4, 10), (sylhet.SortOrder, a.SortOrder, b.SortOrder, c.SortOrder));
    }

    [Fact]
    public async Task Update_KeepingItsOwnNumber_MovesNobody()
    {
        var sylhet = Existing("Sylhet", 30);
        var sajek = Existing("Sajek Valley", 31);

        var result = await UpdateAsync(UpdateCommand(sylhet.Id, [], sortOrder: 30));

        Assert.True(result.IsSuccess);
        Assert.Equal((30, 31), (sylhet.SortOrder, sajek.SortOrder));
    }

    [Fact]
    public async Task Update_WithTheNumberCleared_OfTheLastOne_KeepsItLast()
    {
        Existing("Cox's Bazar", 80);
        var sylhet = Existing("Sylhet", 90);

        await UpdateAsync(UpdateCommand(sylhet.Id, [], sortOrder: null));

        Assert.Equal(80 + DisplayOrder.Step, sylhet.SortOrder); // after the OTHERS - it doesn't count itself
    }

    [Theory]
    [InlineData(null, true)] // empty = at the end
    [InlineData(0, true)]
    [InlineData(-1, false)]
    public void TheForm_AcceptsAnEmptySortOrder_ButNotANegativeOne(int? sortOrder, bool valid) =>
        Assert.Equal(valid, new DestinationFieldsValidator().Validate(CreateCommand([], sortOrder: sortOrder)).IsValid);
}
