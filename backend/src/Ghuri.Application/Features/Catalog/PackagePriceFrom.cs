using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Catalog;

/// <summary>Keeps a fixed package's "from ৳…" price in step with its departures.</summary>
internal static class PackagePriceFrom
{
    /// <summary>
    /// Recalculates the package's PriceFrom after <paramref name="changed"/>
    /// was added, edited or closed. That departure isn't saved yet (saving
    /// happens when the handler's transaction commits), so the database
    /// still has its OLD values: the query skips it, and its NEW values are
    /// counted here in memory instead.
    /// </summary>
    public static async Task RefreshAsync(
        TourPackage package,
        Departure changed,
        IDepartureRepository departures,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var lowest = await departures.LowestOpenAdultPriceAsync(package.Id, today, exceptId: changed.Id, cancellationToken);

        if (changed.Status == DepartureStatus.Open && changed.StartDate >= today)
            lowest = lowest is null ? changed.AdultPrice : Math.Min(lowest.Value, changed.AdultPrice);

        package.SetLowestDeparturePrice(lowest);
    }
}
