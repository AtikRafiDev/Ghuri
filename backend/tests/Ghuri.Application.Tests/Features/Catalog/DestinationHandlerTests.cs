using Ghuri.Application.Features.Catalog.Commands.CreateDestination;
using Ghuri.Application.Features.Catalog.Commands.UpdateDestination;
using Ghuri.Domain.Entities.Catalog;
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

    private static CreateDestinationCommand CreateCommand(IReadOnlyList<Guid> imageFileIds, string name = "Sylhet") =>
        new(Bangladesh, name, Slug: null, Summary: null, imageFileIds, IsFeatured: false, SortOrder: 0);

    private static UpdateDestinationCommand UpdateCommand(Guid id, IReadOnlyList<Guid> imageFileIds) =>
        new(id, Bangladesh, "Sylhet", Slug: null, Summary: null, imageFileIds, IsFeatured: false, SortOrder: 0);

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
}
