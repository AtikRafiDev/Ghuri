using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Catalog.Queries.SearchPackages;

internal sealed class SearchPackagesHandler(IReadDbContext db, IFileStorage storage, TimeProvider clock)
    : IQueryHandler<SearchPackagesQuery, Paged<PackageCardDto>>
{
    public async ValueTask<Result<Paged<PackageCardDto>>> Handle(SearchPackagesQuery query, CancellationToken cancellationToken)
    {
        var today = clock.Today();

        // One SQL statement. LivePrice is the card's "from" price, worked
        // out NOW: flexible = its base price; fixed = the cheapest departure
        // that can still be BOOKED today (null when none) - never the stored
        // PriceFrom, which can still hold a date that has already left.
        // Built OUTSIDE the query below, used inside it (see PackageCovers).
        var bookableDepartures = db.Departures.BookableOn(today);
        var rows =
            from p in db.PublishedPackages()
            join d in db.Destinations on p.DestinationId equals d.Id
            select new
            {
                p,
                DestinationName = d.Name,
                DestinationSlug = d.Slug,
                LivePrice = p.PricingMode == PricingMode.FlexibleStay
                    ? p.BasePrice
                    : bookableDepartures
                        .Where(dep => dep.PackageId == p.Id)
                        .Min(dep => (decimal?)dep.AdultPrice)
            };

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var text = query.Q.Trim();
            rows = rows.Where(r => r.p.Title.Contains(text) || r.DestinationName.Contains(text));
        }
        if (!string.IsNullOrWhiteSpace(query.Destination))
        {
            var destination = Slug.Create(query.Destination);
            rows = rows.Where(r => r.DestinationSlug == destination);
        }
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = Slug.Create(query.Category);
            rows = rows.Where(r => db.PackageCategories.Any(pc =>
                pc.PackageId == r.p.Id && db.Categories.Any(c => c.Id == pc.CategoryId && c.Slug == category)));
        }
        if (query.Mode is { } mode)
            rows = rows.Where(r => r.p.PricingMode == mode);
        // A price filter only matches packages that HAVE a price right now.
        if (query.MinPrice is { } min)
            rows = rows.Where(r => r.LivePrice >= min);
        if (query.MaxPrice is { } max)
            rows = rows.Where(r => r.LivePrice <= max);

        // Packages with no upcoming dates always go last, whatever the sort:
        // they can't be booked right now.
        var ordered = rows.OrderBy(r => r.LivePrice == null ? 1 : 0);
        ordered = query.Sort switch
        {
            PackageSort.PriceLow => ordered.ThenBy(r => r.LivePrice),
            PackageSort.PriceHigh => ordered.ThenByDescending(r => r.LivePrice),
            PackageSort.Newest => ordered.ThenByDescending(r => r.p.PublishedAtUtc),
            _ => ordered.ThenByDescending(r => r.p.IsFeatured).ThenByDescending(r => r.p.PublishedAtUtc)
        };

        var covers = db.PackageCovers();
        var page = await ordered
            .ThenBy(r => r.p.Id) // a fixed order: no card ever shows up on two pages
            .Select(r => new
            {
                r.p.Id, r.p.Slug, r.p.Title, r.p.Summary, r.DestinationName,
                r.p.PricingMode, r.p.DurationDays, r.p.DurationNights, r.p.MinNights, r.p.MaxNights,
                r.LivePrice, r.p.Currency, r.p.IsFeatured,
                CoverKey = covers.Where(c => c.PackageId == r.p.Id).Select(c => c.StorageKey).FirstOrDefault()
            })
            .ToPagedAsync(query.Page, query.PageSize, cancellationToken);

        return page.Map(r => new PackageCardDto(
            r.Id, r.Slug.Value, r.Title, r.Summary, r.DestinationName,
            CoverImageUrl: r.CoverKey is null ? null : storage.GetPublicUrl(r.CoverKey),
            r.PricingMode, r.DurationDays, r.DurationNights, r.MinNights, r.MaxNights,
            PriceFrom: r.LivePrice, r.Currency, r.IsFeatured));
    }
}
