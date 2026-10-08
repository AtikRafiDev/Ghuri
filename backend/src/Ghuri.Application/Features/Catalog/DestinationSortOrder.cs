using Ghuri.Domain.Repositories;
using Ghuri.Domain.Services;

namespace Ghuri.Application.Features.Catalog;

/// <summary>Where a destination being saved goes in the list - one place for Create and Update.</summary>
internal static class DestinationSortOrder
{
    /// <summary>
    /// The sort order the destination gets: the one asked for, or (none
    /// given) after the last destination. If another destination already has
    /// that number, it and any right behind it move along by one
    /// (DisplayOrder.MakeRoom) - saved together with the destination itself.
    /// Call it after every check has passed, so a refused save moves nothing.
    /// </summary>
    /// <param name="destinationId">The destination being edited; null when it's a new one.</param>
    public static async Task<int> PlaceAsync(
        IDestinationRepository destinations,
        int? requested,
        Guid? destinationId,
        CancellationToken cancellationToken)
    {
        var sortOrder = requested
            ?? DisplayOrder.AfterLast(await destinations.GetLastSortOrderAsync(destinationId, cancellationToken));

        DisplayOrder.MakeRoom(sortOrder, await destinations.GetFromSortOrderAsync(sortOrder, destinationId, cancellationToken));
        return sortOrder;
    }
}
