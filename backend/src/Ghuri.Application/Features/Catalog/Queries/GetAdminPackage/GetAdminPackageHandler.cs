using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminPackage;

internal sealed class GetAdminPackageHandler(IReadDbContext db, IFileStorage storage)
    : IQueryHandler<GetAdminPackageQuery, AdminPackageDto>
{
    public async ValueTask<Result<AdminPackageDto>> Handle(GetAdminPackageQuery query, CancellationToken cancellationToken)
    {
        // Unlike the list, this loads the real TourPackage (read-only, not
        // tracked) - so PublishProblems comes from the domain's own
        // GetPublishProblems() and can never disagree with Publish. The three
        // lists load as separate small SELECTs (split queries are the
        // default - see Infrastructure's AddPersistence).
        var package = await db.TourPackages
            .Include(p => p.Images)
            .Include(p => p.ItineraryDays)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);
        if (package is null)
            return CatalogErrors.PackageNotFound;

        var fileIds = package.Images.Select(i => i.FileId).ToList();
        var storageKeys = await db.FileObjects
            .Where(f => fileIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.StorageKey, cancellationToken);

        return new AdminPackageDto(
            package.Id,
            package.PackageCode,
            package.Status,
            package.DestinationId,
            package.Title,
            package.Slug.Value,
            package.Summary,
            package.Description,
            package.TourType,
            package.Categories.Select(c => c.CategoryId).ToList(),
            package.Inclusions,
            package.Exclusions,
            package.TermsAndPolicy,
            package.MinAge,
            package.IsFeatured,
            package.SeoTitle,
            package.SeoDescription,
            package.PricingMode,
            package.DurationDays,
            package.DurationNights,
            package.MinNights,
            package.MaxNights,
            package.BasePrice,
            package.ExtraNightPrice,
            package.MinLeadDays,
            package.PriceFrom,
            package.Currency,
            package.PublishedAtUtc,
            // Images is already in display order (TourPackage sorts it).
            Images: package.Images
                .Select(i => new PackageImageDto(i.FileId, storage.GetPublicUrl(storageKeys[i.FileId])))
                .ToList(),
            ItineraryDays: package.ItineraryDays
                .Select(d => new ItineraryDayDto(d.DayNo, d.Title, d.Description, d.Meals, d.Accommodation))
                .ToList(),
            PublishProblems: package.GetPublishProblems());
    }
}
