using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking.Queries.GetExpiredHolds;

/// <summary>
/// The bookings still waiting for payment whose 20-minute hold is over -
/// what the expiry job works through, oldest first, at most <paramref name="Max"/> per run.
/// </summary>
public sealed record GetExpiredHoldsQuery(int Max) : IQuery<IReadOnlyList<Guid>>;

internal sealed class GetExpiredHoldsHandler(IReadDbContext db, TimeProvider clock)
    : IQueryHandler<GetExpiredHoldsQuery, IReadOnlyList<Guid>>
{
    public async ValueTask<Result<IReadOnlyList<Guid>>> Handle(GetExpiredHoldsQuery query, CancellationToken cancellationToken)
    {
        var nowUtc = clock.GetUtcNow().UtcDateTime;

        // Served by the filtered index on HoldExpiresAtUtc WHERE Status = 1
        // (BookingConfiguration): only pending bookings are even in it.
        var ids = await db.Bookings
            .Where(b => b.Status == BookingStatus.PendingPayment && b.HoldExpiresAtUtc <= nowUtc)
            .OrderBy(b => b.HoldExpiresAtUtc)
            .Select(b => b.Id)
            .Take(query.Max)
            .ToListAsync(cancellationToken);

        return ids;
    }
}
