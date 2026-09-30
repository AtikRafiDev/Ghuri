using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminDestinations;

internal sealed class GetAdminDestinationsHandler(IReadDbContext db, IFileStorage storage)
    : IQueryHandler<GetAdminDestinationsQuery, Paged<AdminDestinationDto>>
{
    public async ValueTask<Result<Paged<AdminDestinationDto>>> Handle(GetAdminDestinationsQuery query, CancellationToken cancellationToken)
    {
        var rows =
            from d in db.Destinations
            join c in db.Countries on d.CountryId equals c.Id
            select new { d, CountryName = c.Name, c.IsoCode };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(r => r.d.Name.Contains(search) || r.CountryName.Contains(search));
        }
        if (query.CountryId is { } countryId)
            rows = rows.Where(r => r.d.CountryId == countryId);
        rows = query.Scope switch
        {
            DestinationScope.National => rows.Where(r => r.IsoCode == HomeCountry.IsoCode),
            DestinationScope.International => rows.Where(r => r.IsoCode != HomeCountry.IsoCode),
            _ => rows
        };

        var page = await rows
            // Id last: two rows with the same order and name still have a
            // fixed order, so no row ever shows up on two pages.
            .OrderBy(r => r.d.SortOrder).ThenBy(r => r.d.Name).ThenBy(r => r.d.Id)
            .Select(r => new
            {
                r.d.Id, r.d.Name, r.d.Slug, r.d.Summary, r.d.CountryId, r.CountryName, r.IsoCode,
                r.d.IsFeatured, r.d.SortOrder, r.d.SeoTitle, r.d.SeoDescription,
                // The whole gallery, in order - loaded for this page's rows only.
                Images = (from i in db.DestinationImages
                          join f in db.FileObjects on i.FileId equals f.Id
                          where i.DestinationId == r.d.Id
                          orderby i.SortOrder
                          select new { i.FileId, f.StorageKey }).ToList(),
                // Deleted packages are skipped automatically (soft-delete filter).
                PackageCount = db.TourPackages.Count(p => p.DestinationId == r.d.Id)
            })
            .ToPagedAsync(query.Page, query.PageSize, cancellationToken);

        return page.Map(r => new AdminDestinationDto(
            r.Id, r.Name, r.Slug.Value, r.Summary, r.CountryId, r.CountryName, r.IsoCode,
            IsInternational: r.IsoCode != HomeCountry.IsoCode,
            Images: r.Images.Select(i => new DestinationImageDto(i.FileId, storage.GetPublicUrl(i.StorageKey))).ToList(),
            r.IsFeatured, r.SortOrder, r.SeoTitle, r.SeoDescription, r.PackageCount));
    }
}
