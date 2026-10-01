using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Catalog.Queries.GetPackageDetails;

internal sealed class GetPackageDetailsHandler(IReadDbContext db, IFileStorage storage, TimeProvider clock)
    : IQueryHandler<GetPackageDetailsQuery, PackageDetailsDto>
{
    public async ValueTask<Result<PackageDetailsDto>> Handle(GetPackageDetailsQuery query, CancellationToken cancellationToken)
    {
        var slug = Slug.Create(query.Slug);
        var package = await db.PublishedPackages()
            .Include(p => p.Images)
            .Include(p => p.ItineraryDays)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
        if (package is null)
            return CatalogErrors.PublicPackageNotFound;

        var destination = await (
                from d in db.Destinations
                join c in db.Countries on d.CountryId equals c.Id
                where d.Id == package.DestinationId
                select new { d.Name, d.Slug, CountryName = c.Name })
            .SingleAsync(cancellationToken);

        var categoryIds = package.Categories.Select(pc => pc.CategoryId).ToList();
        var categories = await db.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Name, c.Slug, c.Icon })
            .ToListAsync(cancellationToken);

        var fileIds = package.Images.Select(i => i.FileId).ToList();
        var storageKeys = await db.FileObjects
            .Where(f => fileIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.StorageKey, cancellationToken);

        var isFlexible = package.PricingMode == PricingMode.FlexibleStay;

        return new PackageDetailsDto(
            package.Id,
            package.Slug.Value,
            package.Title,
            package.Summary,
            package.Description,
            destination.Name,
            destination.Slug.Value,
            destination.CountryName,
            package.TourType,
            categories.Select(c => new PackageCategoryDto(c.Name, c.Slug.Value, c.Icon)).ToList(),
            package.Inclusions,
            package.Exclusions,
            package.TermsAndPolicy,
            package.MinAge,
            package.PricingMode,
            package.DurationDays,
            package.DurationNights,
            package.MinNights,
            package.MaxNights,
            package.BasePrice,
            package.ExtraNightPrice,
            EarliestStartDate: isFlexible ? package.EarliestFlexibleStart(clock.Today()) : null,
            package.Currency,
            // Images is already in display order (TourPackage sorts it) - the first is the cover.
            ImageUrls: package.Images.Select(i => storage.GetPublicUrl(storageKeys[i.FileId])).ToList(),
            Itinerary: package.ItineraryDays
                .Select(d => new PublicItineraryDayDto(d.DayNo, d.Title, d.Description, d.Meals, d.Accommodation))
                .ToList(),
            SeoTitle: package.SeoTitle ?? package.Title,
            SeoDescription: package.SeoDescription ?? package.Summary);
    }
}
