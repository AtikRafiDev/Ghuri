using Ghuri.Application.Abstractions.Data;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.CustomTrips;

/// <summary>One custom trip as both the customer and staff see it (Day 13). Quote is null until staff send one.</summary>
public sealed record CustomTripDto(
    string TripNo,
    CustomTripStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalNights,
    int Adults,
    int Children,
    int Infants,
    HotelLevel HotelLevel,
    decimal? BudgetPerPerson,
    string? Notes,
    string ContactName,
    string ContactPhone,
    string? ContactEmail,
    IReadOnlyList<CustomTripLegDto> Legs,
    CustomTripQuoteDto? Quote,
    CustomTripTimelineDto Timeline,
    bool CanCancel);

public sealed record CustomTripLegDto(int Sequence, Guid DestinationId, string DestinationName, int Nights, TransferMode TransferToNext, DateOnly CheckInDate, DateOnly CheckOutDate);

/// <summary>IsExpired: past ExpiresAtUtc, even if the hourly job hasn't marked it yet.</summary>
public sealed record CustomTripQuoteDto(
    int Version,
    string Itinerary,
    IReadOnlyList<CustomTripQuoteLineDto> Lines,
    decimal Total,
    string Currency,
    DateTime QuotedAtUtc,
    DateTime ExpiresAtUtc,
    bool IsExpired);

public sealed record CustomTripQuoteLineDto(QuoteLineCategory Category, string Description, decimal Amount);

/// <summary>When each step happened - the customer's status timeline (Day 14).</summary>
public sealed record CustomTripTimelineDto(
    DateTime SubmittedAtUtc,
    DateTime? QuotedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? PaidAtUtc,
    DateTime? ExpiredAtUtc,
    DateTime? RejectedAtUtc,
    string? RejectReason,
    DateTime? CancelledAtUtc,
    string? CancelReason);

/// <summary>
/// Reads custom trips for the queries - one place, so the customer's page,
/// the staff page and the lists show the same thing.
/// </summary>
internal sealed class CustomTripReader(IReadDbContext db, TimeProvider clock)
{
    /// <summary>The trip by number; <paramref name="customerId"/> set = only if it's theirs (someone else's is "not there").</summary>
    public async Task<(Guid Id, Guid CustomerId, Guid? QuotedBy, CustomTripDto Trip)?> LoadAsync(
        string tripNo, Guid? customerId, CancellationToken cancellationToken)
    {
        var t = await db.CustomTrips
            .Where(x => x.TripNo == tripNo && (customerId == null || x.CustomerId == customerId))
            .FirstOrDefaultAsync(cancellationToken);
        if (t is null)
            return null;

        var legs = await (
                from l in db.CustomTripLegs
                where l.CustomTripId == t.Id
                join d in db.Destinations.IgnoreQueryFilters() on l.DestinationId equals d.Id // a destination deleted later still names the stop
                orderby l.Sequence
                select new CustomTripLegDto(l.Sequence, l.DestinationId, d.Name, l.Nights, l.TransferToNext, l.CheckInDate, l.CheckOutDate))
            .ToListAsync(cancellationToken);

        CustomTripQuoteDto? quote = null;
        if (t.QuoteVersion > 0 && t.QuoteTotal is { } total && t.QuotedAtUtc is { } quotedAt && t.QuoteExpiresAtUtc is { } expiresAt)
        {
            var lines = await db.CustomTripQuoteLines
                .Where(l => l.CustomTripId == t.Id)
                .OrderBy(l => l.Sequence)
                .Select(l => new CustomTripQuoteLineDto(l.Category, l.Description, l.Amount))
                .ToListAsync(cancellationToken);
            quote = new CustomTripQuoteDto(
                t.QuoteVersion, t.QuoteItinerary ?? string.Empty, lines, total, t.Currency, quotedAt, expiresAt,
                IsExpired: expiresAt <= clock.GetUtcNow().UtcDateTime);
        }

        var dto = new CustomTripDto(
            t.TripNo, t.Status, t.StartDate, t.EndDate, t.TotalNights, t.Adults, t.Children, t.Infants,
            t.HotelLevel, t.BudgetPerPerson, t.Notes, t.ContactName, t.ContactPhone.Value, t.ContactEmail,
            legs, quote,
            new CustomTripTimelineDto(
                t.SubmittedAtUtc, t.QuotedAtUtc, t.AcceptedAtUtc, t.PaidAtUtc, t.ExpiredAtUtc,
                t.RejectedAtUtc, t.RejectReason, t.CancelledAtUtc, t.CancelReason),
            CanCancel: t.Status is CustomTripStatus.Submitted or CustomTripStatus.Quoted or CustomTripStatus.Expired);

        return (t.Id, t.CustomerId, t.QuotedBy, dto);
    }

    /// <summary>"Cox's Bazar → Sylhet" for each trip - one query for a whole page of trips.</summary>
    public async Task<Dictionary<Guid, string>> RoutesAsync(IReadOnlyCollection<Guid> tripIds, CancellationToken cancellationToken)
    {
        var stops = await (
                from l in db.CustomTripLegs
                where tripIds.Contains(l.CustomTripId)
                join d in db.Destinations.IgnoreQueryFilters() on l.DestinationId equals d.Id
                select new { l.CustomTripId, l.Sequence, d.Name })
            .ToListAsync(cancellationToken);

        return stops
            .GroupBy(s => s.CustomTripId)
            .ToDictionary(g => g.Key, g => string.Join(" → ", g.OrderBy(s => s.Sequence).Select(s => s.Name)));
    }
}
