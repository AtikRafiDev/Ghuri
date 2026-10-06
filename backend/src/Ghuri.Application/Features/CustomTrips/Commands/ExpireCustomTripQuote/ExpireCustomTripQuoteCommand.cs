using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.CustomTrips.Commands.ExpireCustomTripQuote;

/// <summary>
/// The quotes whose offer has run out (17-day plan, Day 13: ExpireQuotes,
/// hourly). Read by the expiry job, which then expires them one by one.
/// </summary>
public sealed record GetOverdueQuotesQuery(int Max) : IQuery<IReadOnlyList<Guid>>, IQuietMessage;

internal sealed class GetOverdueQuotesHandler(IReadDbContext db, TimeProvider clock) : IQueryHandler<GetOverdueQuotesQuery, IReadOnlyList<Guid>>
{
    public async ValueTask<Result<IReadOnlyList<Guid>>> Handle(GetOverdueQuotesQuery query, CancellationToken cancellationToken)
    {
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        return await db.CustomTrips
            .Where(t => t.Status == CustomTripStatus.Quoted && t.QuoteExpiresAtUtc <= nowUtc)
            .OrderBy(t => t.QuoteExpiresAtUtc)
            .Take(query.Max)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>One quote ran out: Quoted → Expired. Staff can still send a new quote afterwards.</summary>
/// <remarks>Locked, then re-checked: if staff re-quoted a moment ago, the new deadline stands and nothing happens.</remarks>
public sealed record ExpireCustomTripQuoteCommand(Guid CustomTripId) : ICommand, IQuietMessage;

internal sealed class ExpireCustomTripQuoteHandler(ICustomTripRepository trips, TimeProvider clock) : ICommandHandler<ExpireCustomTripQuoteCommand>
{
    public async ValueTask<Result> Handle(ExpireCustomTripQuoteCommand command, CancellationToken cancellationToken)
    {
        var trip = await trips.GetByIdForUpdateAsync(command.CustomTripId, cancellationToken);
        trip?.ExpireQuote(clock.GetUtcNow().UtcDateTime); // does nothing unless it's really overdue
        return Result.Success();
    }
}
