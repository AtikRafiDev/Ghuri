using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IBookingRepository.</summary>
internal sealed class BookingRepository(AppDbContext db) : IBookingRepository
{
    // Same trick as TourPackageRepository.NextCodeSql: our own constant in
    // the SQL text (a sequence name can't be a parameter), "AS [Value]" for EF.
    private const string NextNoSql =
        "SELECT NEXT VALUE FOR [booking].[" + AppDbContext.BookingNoSequenceName + "] AS [Value]";

    public async Task<string> NextBookingNoAsync(CancellationToken cancellationToken)
    {
        // ToListAsync, not SingleAsync - see TourPackageRepository.NextPackageCodeAsync.
        var values = await db.Database.SqlQueryRaw<int>(NextNoSql).ToListAsync(cancellationToken);
        return $"TB{values.Single()}";
    }

    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Booking?> GetByBookingNoAsync(string bookingNo, CancellationToken cancellationToken) =>
        db.Bookings.FirstOrDefaultAsync(b => b.BookingNo == bookingNo, cancellationToken);

    // UPDLOCK = "I'm about to change this row": others wanting to change it
    // wait until our transaction ends (same idea as EfIdempotencyStore).
    // FromSqlInterpolated turns {id} into a SQL parameter.
    public async Task<Booking?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var rows = await db.Bookings
            .FromSqlInterpolated($"SELECT * FROM [booking].[Bookings] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public async Task<Booking?> GetByBookingNoForUpdateAsync(string bookingNo, CancellationToken cancellationToken)
    {
        var rows = await db.Bookings
            .FromSqlInterpolated($"SELECT * FROM [booking].[Bookings] WITH (UPDLOCK, ROWLOCK) WHERE [BookingNo] = {bookingNo}")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public Task<int> CountUnpaidHoldsAsync(Guid customerId, DateTime nowUtc, CancellationToken cancellationToken) =>
        db.Bookings.CountAsync(
            b => b.CustomerId == customerId && b.Status == BookingStatus.PendingPayment && b.HoldExpiresAtUtc > nowUtc,
            cancellationToken);

    public void Add(Booking booking) => db.Bookings.Add(booking);
}
