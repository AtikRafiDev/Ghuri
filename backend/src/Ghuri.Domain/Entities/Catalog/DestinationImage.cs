using Ghuri.Domain.Common;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// One photo in a destination's gallery (catalog.DestinationImages). Only
/// Destination creates or changes these (internal members) - a photo can't
/// exist without its destination.
/// </summary>
public sealed class DestinationImage : BaseEntity
{
    public Guid DestinationId { get; private set; }

    /// <summary>FK to ops.FileObjects - the uploaded WebP.</summary>
    public Guid FileId { get; private set; }

    /// <summary>0 = the cover (shown on cards and lists), then the gallery order.</summary>
    public int SortOrder { get; private set; }

    private DestinationImage()
    {
    }

    internal static DestinationImage Create(Guid destinationId, Guid fileId, int sortOrder) =>
        new() { DestinationId = destinationId, FileId = fileId, SortOrder = sortOrder };

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;
}
