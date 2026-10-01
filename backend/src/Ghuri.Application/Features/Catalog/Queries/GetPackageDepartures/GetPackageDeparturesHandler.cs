using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Catalog.Queries.GetPackageDepartures;

internal sealed class GetPackageDeparturesHandler(IReadDbContext db, TimeProvider clock)
    : IQueryHandler<GetPackageDeparturesQuery, IReadOnlyList<AdminDepartureDto>>
{
    public async ValueTask<Result<IReadOnlyList<AdminDepartureDto>>> Handle(
        GetPackageDeparturesQuery query, CancellationToken cancellationToken)
    {
        // 404 for an unknown package, rather than an empty list that looks like "no dates yet".
        if (!await db.TourPackages.AnyAsync(p => p.Id == query.PackageId, cancellationToken))
            return CatalogErrors.PackageNotFound;

        var today = clock.Today();
        var rows = await db.Departures
            .Where(d => d.PackageId == query.PackageId)
            .OrderBy(d => d.StartDate)
            .Select(d => new AdminDepartureDto(
                d.Id, d.StartDate, d.EndDate,
                d.AdultPrice, d.ChildPrice, d.InfantPrice, d.SingleSupplement,
                d.TotalSeats, d.ReservedSeats,
                // Spelled out (not d.SeatsLeft): SQL can't run a C# property, only columns.
                d.TotalSeats - d.ReservedSeats,
                d.BookingCutoffDays, d.Status,
                d.StartDate < today))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
