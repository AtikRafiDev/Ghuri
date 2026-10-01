using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Queries.SearchPackages;

/// <summary>
/// The public package list: the search page (Day 7) and the home page's
/// "popular packages" (sort = Recommended, first 8). Only published packages.
/// </summary>
/// <param name="Q">Free text: matches the title or the destination's name.</param>
/// <param name="Destination">A destination's slug, e.g. "cox-s-bazar".</param>
/// <param name="Category">A category's slug, e.g. "beach".</param>
/// <param name="MinPrice">"From" price per adult, in taka.</param>
/// <param name="MaxPrice">"From" price per adult, in taka.</param>
/// <param name="Mode">Only fixed departures, or only flexible stays.</param>
/// <param name="Sort">Order of the results.</param>
/// <param name="Page">1-based.</param>
/// <param name="PageSize">Cards per page.</param>
public sealed record SearchPackagesQuery(
    string? Q,
    string? Destination,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    PricingMode? Mode,
    PackageSort Sort = PackageSort.Recommended,
    int Page = 1,
    int PageSize = 12) : IQuery<Paged<PackageCardDto>>;

public enum PackageSort
{
    /// <summary>Featured first, then the newest.</summary>
    Recommended = 0,
    PriceLow = 1,
    PriceHigh = 2,
    Newest = 3
}
