using Ghuri.Application.Abstractions.Data;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Catalog.Queries;

/// <summary>A package's cover photo with where its file is stored. Init properties, not a record - see DestinationPhoto.</summary>
internal sealed class PackageCover
{
    public Guid PackageId { get; init; }
    public string StorageKey { get; init; } = string.Empty;
}

/// <summary>The rules every PUBLIC package query shares, in one place.</summary>
internal static class PublicPackageQueries
{
    /// <summary>
    /// What the public may see: published packages only. A draft or
    /// archived package simply doesn't exist for them (404, not 403 -
    /// "forbidden" would admit it exists). Deleted ones are already
    /// hidden by the soft-delete filter.
    /// </summary>
    public static IQueryable<TourPackage> PublishedPackages(this IReadDbContext db) =>
        db.TourPackages.Where(p => p.Status == PackageStatus.Published);

    /// <summary>
    /// Departures a customer can still book today: open, and today is on or
    /// before the last booking day. The SQL form of Departure.CheckBookable's
    /// date rules - "LastBookingDate >= today" is the same as
    /// "StartDate >= today + BookingCutoffDays", which SQL can use directly.
    /// Seats aren't checked: a sold-out date is still shown, as "Sold out".
    /// </summary>
    public static IQueryable<Departure> BookableOn(this IQueryable<Departure> departures, DateOnly today) =>
        departures.Where(d => d.Status == DepartureStatus.Open && d.StartDate >= today.AddDays(d.BookingCutoffDays));

    /// <summary>
    /// Every package's cover (gallery position 0) joined to its file. Call
    /// it OUTSIDE a query and use the result inside, like DestinationPhotos:
    /// EF Core can't look inside a method called in the middle of a query,
    /// but it can use a query stored in a variable as a subquery.
    /// </summary>
    public static IQueryable<PackageCover> PackageCovers(this IReadDbContext db) =>
        from image in db.PackageImages
        join file in db.FileObjects on image.FileId equals file.Id
        where image.SortOrder == 0
        select new PackageCover { PackageId = image.PackageId, StorageKey = file.StorageKey };
}
