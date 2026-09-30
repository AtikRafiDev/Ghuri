using Ghuri.Application.Abstractions.Data;

namespace Ghuri.Application.Features.Catalog.Queries;

/// <summary>One gallery photo together with where its file is stored.</summary>
/// <remarks>
/// A class with init properties (not a positional record) on purpose: EF Core
/// can keep filtering and sorting on a projection written as
/// "new DestinationPhoto { ... }", but not on constructor arguments.
/// </remarks>
internal sealed class DestinationPhoto
{
    public Guid DestinationId { get; init; }
    public Guid FileId { get; init; }

    /// <summary>0 = the cover.</summary>
    public int SortOrder { get; init; }

    public string StorageKey { get; init; } = string.Empty;
}

internal static class DestinationPhotoQueries
{
    /// <summary>
    /// Every destination photo joined to its file - the one definition the
    /// destination queries share. Call it once OUTSIDE a query and use the
    /// result inside: EF Core turns it into a subquery of the same SQL
    /// statement (no extra round trip per destination).
    /// </summary>
    public static IQueryable<DestinationPhoto> DestinationPhotos(this IReadDbContext db) =>
        from image in db.DestinationImages
        join file in db.FileObjects on image.FileId equals file.Id
        select new DestinationPhoto
        {
            DestinationId = image.DestinationId,
            FileId = image.FileId,
            SortOrder = image.SortOrder,
            StorageKey = file.StorageKey,
        };
}
