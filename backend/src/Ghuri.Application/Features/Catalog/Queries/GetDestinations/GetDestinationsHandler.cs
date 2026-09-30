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
        // One SQL query: destination + its country + its cover photo (the
        // gallery's first one; null when it has no photos yet).
        var photos = db.DestinationPhotos();
        var rows =
            from d in db.Destinations
            join c in db.Countries on d.CountryId equals c.Id
            select new
            {
                d,
                CountryName = c.Name,
                c.IsoCode,
                ImageKey = photos.Where(p => p.DestinationId == d.Id).OrderBy(p => p.SortOrder)
                    .Select(p => p.StorageKey).FirstOrDefault()
            };

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
