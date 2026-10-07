using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.Entities.Catalog;

public class DestinationTests
{
    private static readonly Guid PhotoA = Guid.NewGuid();
    private static readonly Guid PhotoB = Guid.NewGuid();
    private static readonly Guid PhotoC = Guid.NewGuid();

    private static Destination NewDestination() => Destination.Create(19, "Sylhet", Slug.Create("Sylhet"));

    private static Guid[] Gallery(Destination destination) => destination.Images.Select(i => i.FileId).ToArray();

    [Fact]
    public void SetImages_KeepsTheGivenOrder_TheFirstIsTheCover()
    {
        var destination = NewDestination();

        destination.SetImages([PhotoB, PhotoA]);

        Assert.Equal([PhotoB, PhotoA], Gallery(destination));
        Assert.Equal([0, 1], destination.Images.Select(i => i.SortOrder));
        Assert.All(destination.Images, image => Assert.Equal(destination.Id, image.DestinationId));
    }

    [Fact]
    public void SetImages_Reordering_KeepsTheSamePhotoRows_AndOnlyChangesTheirPosition()
    {
        var destination = NewDestination();
        destination.SetImages([PhotoA, PhotoB]);
        var rowIdsBefore = destination.Images.ToDictionary(i => i.FileId, i => i.Id);

        destination.SetImages([PhotoB, PhotoA]);

        Assert.Equal([PhotoB, PhotoA], Gallery(destination));
        // Same rows, not deleted and re-added - so the database only UPDATEs SortOrder.
        Assert.All(destination.Images, image => Assert.Equal(rowIdsBefore[image.FileId], image.Id));
    }

    [Fact]
    public void SetImages_AddsNewPhotos_AndRemovesMissingOnes()
    {
        var destination = NewDestination();
        destination.SetImages([PhotoA, PhotoB]);

        destination.SetImages([PhotoC, PhotoA]);

        Assert.Equal([PhotoC, PhotoA], Gallery(destination));
        Assert.Equal([0, 1], destination.Images.Select(i => i.SortOrder));
    }

    [Fact]
    public void SetImages_EmptyList_RemovesEveryPhoto()
    {
        var destination = NewDestination();
        destination.SetImages([PhotoA, PhotoB]);

        destination.SetImages([]);

        Assert.Empty(destination.Images);
    }

    [Fact]
    public void SetImages_MoreThanTheMaximum_IsRefused_AndChangesNothing()
    {
        var destination = NewDestination();
        destination.SetImages([PhotoA]);
        var tooMany = Enumerable.Range(0, Destination.MaxImages + 1).Select(_ => Guid.NewGuid()).ToList();

        Assert.Throws<ArgumentException>(() => destination.SetImages(tooMany));
        Assert.Equal([PhotoA], Gallery(destination));
    }

    [Fact]
    public void SetImages_TheSamePhotoTwice_IsRefused()
    {
        var destination = NewDestination();

        Assert.Throws<ArgumentException>(() => destination.SetImages([PhotoA, PhotoA]));
        Assert.Empty(destination.Images);
    }

    [Fact]
    public void Update_TrimsText_AndStoresEmptyOptionalFieldsAsNull()
    {
        var destination = NewDestination();

        destination.Update(19, "  Sylhet  ", Slug.Create("Sylhet"), "   ", false, 0);

        Assert.Equal("Sylhet", destination.Name);
        Assert.Null(destination.Summary);
    }
}
