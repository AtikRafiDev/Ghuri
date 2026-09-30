using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Catalog.Queries.GetDestinations;

internal sealed class GetDestinationsHandler(IReadDbContext db, IFileStorage storage)
    : IQueryHandler<GetDestinationsQuery, IReadOnlyList<DestinationDto>>
{
    public async ValueTask<Result<IReadOnlyList<DestinationDto>>> Handle(GetDestinationsQuery query, CancellationToken cancellationToken)
    {
        // One SQL query: destination + its country + its cover image (LEFT
        // JOIN - a destination without an image still appears).
        var rows =
            from d in db.Destinations
            join c in db.Countries on d.CountryId equals c.Id
            join f in db.FileObjects on d.ImageFileId equals (Guid?)f.Id into images
            from f in images.DefaultIfEmpty()
            select new { d, CountryName = c.Name, c.IsoCode, ImageKey = f == null ? null : f.StorageKey };

        rows = query.Scope switch
        {
            DestinationScope.National => rows.Where(r => r.IsoCode == HomeCountry.IsoCode),
            DestinationScope.International => rows.Where(r => r.IsoCode != HomeCountry.IsoCode),
            _ => rows
        };
        if (query.FeaturedOnly)
            rows = rows.Where(r => r.d.IsFeatured);

        var list = await rows
            .OrderBy(r => r.d.SortOrder).ThenBy(r => r.d.Name)
            .Select(r => new { r.d.Id, r.d.Name, r.d.Slug, r.d.Summary, r.CountryName, r.IsoCode, r.ImageKey, r.d.IsFeatured })
            .ToListAsync(cancellationToken);

        // In memory: the URL format is a storage detail SQL knows nothing about.
        return list
            .Select(r => new DestinationDto(
                r.Id, r.Name, r.Slug.Value, r.Summary, r.CountryName, r.IsoCode,
                IsInternational: r.IsoCode != HomeCountry.IsoCode,
                ImageUrl: r.ImageKey is null ? null : storage.GetPublicUrl(r.ImageKey),
                r.IsFeatured))
            .ToList();
    }
}
