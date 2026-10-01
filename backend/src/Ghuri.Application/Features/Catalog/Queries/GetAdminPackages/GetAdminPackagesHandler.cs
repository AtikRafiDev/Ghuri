using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminPackages;

internal sealed class GetAdminPackagesHandler(IReadDbContext db, IFileStorage storage)
    : IQueryHandler<GetAdminPackagesQuery, Paged<AdminPackageListItemDto>>
{
    public async ValueTask<Result<Paged<AdminPackageListItemDto>>> Handle(GetAdminPackagesQuery query, CancellationToken cancellationToken)
    {
        var rows =
            from p in db.TourPackages
            join d in db.Destinations on p.DestinationId equals d.Id
            select new { p, DestinationName = d.Name };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(r => r.p.Title.Contains(search) || r.p.PackageCode.Contains(search));
        }
        if (query.DestinationId is { } destinationId)
            rows = rows.Where(r => r.p.DestinationId == destinationId);
        if (query.Status is { } status)
            rows = rows.Where(r => r.p.Status == status);
        if (query.PricingMode is { } pricingMode)
            rows = rows.Where(r => r.p.PricingMode == pricingMode);

        var page = await rows
            // CreatedAtUtc is a "shadow" column (set by the audit interceptor,
            // not a C# property), so it's reached through EF.Property. Id last:
            // a fixed order, so no row ever shows up on two pages.
            .OrderByDescending(r => EF.Property<DateTime>(r.p, "CreatedAtUtc")).ThenBy(r => r.p.Id)
            .Select(r => new
            {
                r.p.Id, r.p.PackageCode, r.p.Title, r.p.Slug, r.DestinationName, r.p.PricingMode, r.p.Status,
                r.p.DurationDays, r.p.DurationNights, r.p.MinNights, r.p.MaxNights,
                r.p.PriceFrom, r.p.Currency, r.p.IsFeatured, r.p.PublishedAtUtc,
                // Only the cover (SortOrder 0), and only for this page's rows.
                CoverKey = (
                    from image in db.PackageImages
                    join file in db.FileObjects on image.FileId equals file.Id
                    where image.PackageId == r.p.Id && image.SortOrder == 0
                    select file.StorageKey).FirstOrDefault()
            })
            .ToPagedAsync(query.Page, query.PageSize, cancellationToken);

        return page.Map(r => new AdminPackageListItemDto(
            r.Id, r.PackageCode, r.Title, r.Slug.Value, r.DestinationName, r.PricingMode, r.Status,
            r.DurationDays, r.DurationNights, r.MinNights, r.MaxNights,
            r.PriceFrom, r.Currency, r.IsFeatured,
            CoverImageUrl: r.CoverKey is null ? null : storage.GetPublicUrl(r.CoverKey),
            r.PublishedAtUtc));
    }
}
