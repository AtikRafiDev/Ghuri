using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Catalog.Queries.GetDepartureAvailability;

internal sealed class GetDepartureAvailabilityHandler(IReadDbContext db, TimeProvider clock)
    : IQueryHandler<GetDepartureAvailabilityQuery, IReadOnlyList<AvailableDepartureDto>>
{
    public async ValueTask<Result<IReadOnlyList<AvailableDepartureDto>>> Handle(
        GetDepartureAvailabilityQuery query, CancellationToken cancellationToken)
    {
        var slug = Slug.Create(query.Slug);
        var packageId = await db.PublishedPackages()
            .Where(p => p.Slug == slug)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (packageId is null)
            return CatalogErrors.PublicPackageNotFound;

        var today = clock.Today();

        // Only dates the quote would accept today (open, booking not closed),
        // sold-out ones included so the picker can show "Sold out".
        // (A flexible package has no departures: the list is simply empty.)
        var bookable = await db.Departures.BookableOn(today)
            .Where(d => d.PackageId == packageId)
            .OrderBy(d => d.StartDate)
            .ToListAsync(cancellationToken);

        return bookable
            .Select(d => new AvailableDepartureDto(
                d.Id, d.StartDate, d.EndDate,
                d.AdultPrice, d.ChildPrice, d.InfantPrice, d.SingleSupplement,
                d.SeatsLeft, d.LastBookingDate))
            .ToList();
    }
}
