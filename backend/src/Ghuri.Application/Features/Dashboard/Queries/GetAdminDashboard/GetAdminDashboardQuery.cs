using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Application.Features.Dashboard.Queries.GetAdminDashboard;

/// <summary>
/// The admin home's cards (17-day plan, Day 12: GetAdminDashboard -
/// "today's bookings, revenue, pending payments, upcoming trips"), plus the
/// refunds still waiting for staff and the charts' history. "Today" and
/// "this month" are Bangladesh dates.
/// </summary>
public sealed record GetAdminDashboardQuery : IQuery<AdminDashboardDto>;

/// <summary>
/// Revenue = money received (succeeded payments, by the day they were paid)
/// minus money given back (completed refunds, by the day they were sent).
/// PendingPayments = bookings still inside their 20-minute hold.
/// UpcomingTrips = confirmed trips starting in the next 7 days, soonest first.
/// CustomTripsToQuote = custom trip requests no one has priced yet.
/// Last14Days = bookings made and revenue per day, oldest first, today last
/// (every day present, zeros included - a chart needs the gaps too).
/// StatusMix / TopPackages = bookings MADE in the last 30 days: what became
/// of them, and which packages sold most (paid or part-paid bookings only).
/// </summary>
public sealed record AdminDashboardDto(
    DateOnly Today,
    int BookingsToday,
    decimal RevenueToday,
    decimal RevenueThisMonth,
    string Currency,
    int PendingPayments,
    int RefundsToProcess,
    decimal RefundsToProcessAmount,
    int UpcomingTripCount,
    IReadOnlyList<UpcomingTripDto> UpcomingTrips,
    int CustomTripsToQuote,
    IReadOnlyList<DailyStatDto> Last14Days,
    IReadOnlyList<StatusCountDto> StatusMix,
    IReadOnlyList<TopPackageDto> TopPackages);

public sealed record UpcomingTripDto(string BookingNo, string? PackageTitle, string ContactName, string ContactPhone, DateOnly StartDate, int Travellers);

public sealed record DailyStatDto(DateOnly Date, int Bookings, decimal Revenue);

public sealed record StatusCountDto(BookingStatus Status, int Count);

public sealed record TopPackageDto(string Title, string Slug, int Bookings, decimal Amount);

internal sealed class GetAdminDashboardHandler(IReadDbContext db, TimeProvider clock) : IQueryHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    /// <summary>How far ahead "upcoming" looks, and how many trips the card lists.</summary>
    private const int UpcomingDays = 7;
    private const int UpcomingListed = 10;

    /// <summary>The trend chart's length, and the window the status mix and top packages look back over.</summary>
    private const int SeriesDays = 14;
    private const int MixDays = 30;
    private const int TopPackagesListed = 5;

    public async ValueTask<Result<AdminDashboardDto>> Handle(GetAdminDashboardQuery query, CancellationToken cancellationToken)
    {
        var today = clock.Today();
        var todayStartUtc = AgencyDay.StartUtc(today);
        var tomorrowStartUtc = AgencyDay.StartUtc(today.AddDays(1));
        var monthStartUtc = AgencyDay.StartUtc(new DateOnly(today.Year, today.Month, 1));

        var bookingsToday = await db.Bookings.CountAsync(
            b => EF.Property<DateTime>(b, "CreatedAtUtc") >= todayStartUtc && EF.Property<DateTime>(b, "CreatedAtUtc") < tomorrowStartUtc,
            cancellationToken);

        var revenueToday = await RevenueAsync(todayStartUtc, tomorrowStartUtc, cancellationToken);
        var revenueThisMonth = await RevenueAsync(monthStartUtc, tomorrowStartUtc, cancellationToken);

        var pendingPayments = await db.Bookings.CountAsync(b => b.Status == BookingStatus.PendingPayment, cancellationToken);

        var openRefunds = db.Refunds.Where(r => // = Refund.IsOpen: money still owed
            r.Status == RefundStatus.Requested || r.Status == RefundStatus.Approved
            || r.Status == RefundStatus.Processing || r.Status == RefundStatus.Failed);
        var refundsToProcess = await openRefunds.CountAsync(cancellationToken);
        var refundsToProcessAmount = await openRefunds.SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0;

        var lastDay = today.AddDays(UpcomingDays);
        var upcoming =
            from b in db.Bookings
            where b.Status == BookingStatus.Confirmed && b.StartDate >= today && b.StartDate <= lastDay
            join p in db.TourPackages on b.PackageId equals p.Id into packages
            from p in packages.DefaultIfEmpty()
            select new { b, PackageTitle = p == null ? null : p.Title };
        var upcomingCount = await upcoming.CountAsync(cancellationToken);
        var upcomingRows = await upcoming
            .OrderBy(r => r.b.StartDate).ThenBy(r => r.b.BookingNo)
            .Take(UpcomingListed)
            .Select(r => new
            {
                r.b.BookingNo, r.PackageTitle, r.b.ContactName, r.b.ContactPhone, r.b.StartDate,
                Travellers = r.b.Adults + r.b.Children + r.b.Infants
            })
            .ToListAsync(cancellationToken);

        var customTripsToQuote = await db.CustomTrips.CountAsync(t => t.Status == CustomTripStatus.Submitted, cancellationToken);

        var last14Days = await DailySeriesAsync(today, tomorrowStartUtc, cancellationToken);

        var mixStartUtc = AgencyDay.StartUtc(today.AddDays(-(MixDays - 1)));
        var recentBookings = db.Bookings.Where(b => EF.Property<DateTime>(b, "CreatedAtUtc") >= mixStartUtc);
        var statusMix = await recentBookings
            .GroupBy(b => b.Status)
            .Select(g => new StatusCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var topPackages = await TopPackagesAsync(recentBookings, cancellationToken);

        return new AdminDashboardDto(
            today, bookingsToday, revenueToday, revenueThisMonth, "BDT",
            pendingPayments, refundsToProcess, refundsToProcessAmount,
            upcomingCount,
            upcomingRows.Select(r => new UpcomingTripDto(r.BookingNo, r.PackageTitle, r.ContactName, r.ContactPhone.Value, r.StartDate, r.Travellers)).ToList(),
            customTripsToQuote,
            last14Days,
            statusMix.OrderBy(s => s.Status).ToList(),
            topPackages);
    }

    /// <summary>Received minus given back, in [fromUtc, toUtc). A refund counts against the day it was SENT.</summary>
    private async Task<decimal> RevenueAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var received = await ReceivedPayments(fromUtc, toUtc).SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;
        var refunded = await CompletedRefunds(fromUtc, toUtc).SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0;
        return received - refunded;
    }

    /// <summary>
    /// The last 14 Bangladesh days, one row each. The few hundred rows involved
    /// are fetched and bucketed by day in memory: grouping a UTC column by
    /// LOCAL date in SQL would need the +6h shift inside the GROUP BY.
    /// </summary>
    private async Task<List<DailyStatDto>> DailySeriesAsync(DateOnly today, DateTime toUtc, CancellationToken cancellationToken)
    {
        var firstDay = today.AddDays(-(SeriesDays - 1));
        var fromUtc = AgencyDay.StartUtc(firstDay);

        var bookedAt = await db.Bookings
            .Where(b => EF.Property<DateTime>(b, "CreatedAtUtc") >= fromUtc && EF.Property<DateTime>(b, "CreatedAtUtc") < toUtc)
            .Select(b => EF.Property<DateTime>(b, "CreatedAtUtc"))
            .ToListAsync(cancellationToken);
        var received = await ReceivedPayments(fromUtc, toUtc)
            .Select(p => new { At = p.PaidAtUtc!.Value, p.Amount })
            .ToListAsync(cancellationToken);
        var refunded = await CompletedRefunds(fromUtc, toUtc)
            .Select(r => new { At = r.CompletedAtUtc!.Value, r.Amount })
            .ToListAsync(cancellationToken);

        var bookingsByDay = bookedAt.GroupBy(AgencyDay.Of).ToDictionary(g => g.Key, g => g.Count());
        var revenueByDay = received.Select(p => (Day: AgencyDay.Of(p.At), p.Amount))
            .Concat(refunded.Select(r => (Day: AgencyDay.Of(r.At), Amount: -r.Amount)))
            .GroupBy(x => x.Day)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        return Enumerable.Range(0, SeriesDays)
            .Select(i => firstDay.AddDays(i))
            .Select(day => new DailyStatDto(day, bookingsByDay.GetValueOrDefault(day), revenueByDay.GetValueOrDefault(day)))
            .ToList();
    }

    /// <summary>The best sellers among the given bookings: paid or part-paid, most bookings first (then most money).</summary>
    private async Task<List<TopPackageDto>> TopPackagesAsync(IQueryable<BookingEntity> bookings, CancellationToken cancellationToken)
    {
        var sold = await bookings
            .Where(b => b.PackageId != null &&
                        (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.PartiallyPaid || b.Status == BookingStatus.Completed))
            .GroupBy(b => b.PackageId!.Value)
            .Select(g => new { PackageId = g.Key, Bookings = g.Count(), Amount = g.Sum(b => b.TotalAmount) })
            .OrderByDescending(x => x.Bookings).ThenByDescending(x => x.Amount)
            .Take(TopPackagesListed)
            .ToListAsync(cancellationToken);

        var ids = sold.Select(s => s.PackageId).ToList();
        var packages = await db.TourPackages
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Slug })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return sold
            .Where(s => packages.ContainsKey(s.PackageId))
            .Select(s => new TopPackageDto(packages[s.PackageId].Title, packages[s.PackageId].Slug.Value, s.Bookings, s.Amount))
            .ToList();
    }

    // Refunded payments still count as received - the refund is subtracted on its own day.
    private IQueryable<Payment> ReceivedPayments(DateTime fromUtc, DateTime toUtc) =>
        db.Payments.Where(p => (p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.Refunded || p.Status == PaymentStatus.PartiallyRefunded)
                               && p.PaidAtUtc >= fromUtc && p.PaidAtUtc < toUtc);

    private IQueryable<Refund> CompletedRefunds(DateTime fromUtc, DateTime toUtc) =>
        db.Refunds.Where(r => r.Status == RefundStatus.Completed && r.CompletedAtUtc >= fromUtc && r.CompletedAtUtc < toUtc);
}
