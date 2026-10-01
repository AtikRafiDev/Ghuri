using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IDepartureRepository.</summary>
internal sealed class DepartureRepository(AppDbContext db) : IDepartureRepository
{
    public Task<Departure?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Departures.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Departure>> ListForPackageAsync(Guid packageId, CancellationToken cancellationToken) =>
        await db.Departures.Where(d => d.PackageId == packageId).ToListAsync(cancellationToken);

    public Task<bool> ExistsOnDateAsync(Guid packageId, DateOnly startDate, Guid? exceptId, CancellationToken cancellationToken) =>
        db.Departures.AnyAsync(d => d.PackageId == packageId && d.StartDate == startDate && d.Id != exceptId, cancellationToken);

    public Task<decimal?> LowestOpenAdultPriceAsync(Guid packageId, DateOnly today, Guid? exceptId, CancellationToken cancellationToken) =>
        db.Departures
            .Where(d => d.PackageId == packageId && d.Status == DepartureStatus.Open && d.StartDate >= today && d.Id != exceptId)
            // (decimal?) so MIN over zero rows gives null instead of throwing.
            .MinAsync(d => (decimal?)d.AdultPrice, cancellationToken);

    public Task<bool> HasOpenUpcomingAsync(Guid packageId, DateOnly today, CancellationToken cancellationToken) =>
        db.Departures.AnyAsync(
            d => d.PackageId == packageId && d.Status == DepartureStatus.Open && d.StartDate >= today, cancellationToken);

    /// <summary>
    /// One SQL statement - the check and the change happen together:
    ///   UPDATE catalog.Departures SET ReservedSeats = ReservedSeats + @seats
    ///   WHERE Id = @id AND Status = 1 AND ReservedSeats + @seats &lt;= TotalSeats
    /// SQL Server locks the row while it runs, so 20 requests for the last 5
    /// seats queue up on it: the first 5 find room and change 1 row each,
    /// the other 15 find the WHERE no longer true and change 0 rows.
    /// Reading the seats first and saving later (load → check → save) would
    /// let two requests both read "1 seat left" and both take it.
    /// </summary>
    public async Task<bool> TryReserveSeatsAsync(Guid departureId, short seats, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seats);

        var changedRows = await db.Departures
            .Where(d => d.Id == departureId
                && d.Status == DepartureStatus.Open
                && d.ReservedSeats + seats <= d.TotalSeats)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.ReservedSeats, d => (short)(d.ReservedSeats + seats)), cancellationToken);

        return changedRows == 1;
    }

    /// <summary>The same idea in reverse; the WHERE stops the count going below 0.</summary>
    public async Task<bool> ReleaseSeatsAsync(Guid departureId, short seats, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seats);

        var changedRows = await db.Departures
            .Where(d => d.Id == departureId && d.ReservedSeats >= seats)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.ReservedSeats, d => (short)(d.ReservedSeats - seats)), cancellationToken);

        return changedRows == 1;
    }

    public void Add(Departure departure) => db.Departures.Add(departure);
}
