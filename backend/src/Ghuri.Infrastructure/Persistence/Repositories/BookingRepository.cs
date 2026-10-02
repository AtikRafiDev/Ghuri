using Ghuri.Domain.Entities.Booking;
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

    public void Add(Booking booking) => db.Bookings.Add(booking);
}
