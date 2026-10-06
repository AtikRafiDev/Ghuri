using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of ICustomTripRepository.</summary>
internal sealed class CustomTripRepository(AppDbContext db) : ICustomTripRepository
{
    // Same trick as BookingRepository: our own constant in the SQL text, "AS [Value]" for EF.
    private const string NextNoSql =
        "SELECT NEXT VALUE FOR [booking].[" + AppDbContext.CustomTripNoSequenceName + "] AS [Value]";

    public async Task<string> NextTripNoAsync(CancellationToken cancellationToken)
    {
        var values = await db.Database.SqlQueryRaw<int>(NextNoSql).ToListAsync(cancellationToken);
        return $"CT{values.Single()}";
    }

    // UPDLOCK on the trip row (see BookingRepository.GetByIdForUpdateAsync);
    // its legs and quote lines come along - a re-quote replaces the lines.
    public async Task<CustomTrip?> GetByTripNoForUpdateAsync(string tripNo, CancellationToken cancellationToken)
    {
        var rows = await db.CustomTrips
            .FromSqlInterpolated($"SELECT * FROM [booking].[CustomTrips] WITH (UPDLOCK, ROWLOCK) WHERE [TripNo] = {tripNo}")
            .Include(t => t.Legs)
            .Include(t => t.QuoteLines)
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public async Task<CustomTrip?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var rows = await db.CustomTrips
            .FromSqlInterpolated($"SELECT * FROM [booking].[CustomTrips] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .Include(t => t.Legs)
            .Include(t => t.QuoteLines)
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public void Add(CustomTrip trip) => db.CustomTrips.Add(trip);
}
