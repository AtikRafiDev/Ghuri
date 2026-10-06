using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Dashboard.Queries.GetAdminDashboard;

/// <summary>
/// The admin home's cards (17-day plan, Day 12: GetAdminDashboard -
/// "today's bookings, revenue, pending payments, upcoming trips"), plus the
/// refunds still waiting for staff. "Today" and "this month" are Bangladesh dates.
/// </summary>
public sealed record GetAdminDashboardQuery : IQuery<AdminDashboardDto>;

/// <summary>
/// Revenue = money received (succeeded payments, by the day they were paid)
/// minus money given back (completed refunds, by the day they were sent).
/// PendingPayments = bookings still inside their 20-minute hold.
/// UpcomingTrips = confirmed trips starting in the next 7 days, soonest first.
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
    IReadOnlyList<UpcomingTripDto> UpcomingTrips);

public sealed record UpcomingTripDto(string BookingNo, string? PackageTitle, string ContactName, string ContactPhone, DateOnly StartDate, int Travellers);

internal sealed class GetAdminDashboardHandler(IReadDbContext db, TimeProvider clock) : IQueryHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    /// <summary>How far ahead "upcoming" looks, and how many trips the card lists.</summary>
    private const int UpcomingDays = 7;
    private const int UpcomingListed = 10;

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

        var openRefunds = db.Refunds.Where(r =>
            r.Status == RefundStatus.Requested || r.Status == RefundStatus.Approved || r.Status == RefundStatus.Processing);
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

        return new AdminDashboardDto(
            today, bookingsToday, revenueToday, revenueThisMonth, "BDT",
            pendingPayments, refundsToProcess, refundsToProcessAmount,
            upcomingCount,
            upcomingRows.Select(r => new UpcomingTripDto(r.BookingNo, r.PackageTitle, r.ContactName, r.ContactPhone.Value, r.StartDate, r.Travellers)).ToList());
    }

    /// <summary>Received minus given back, in [fromUtc, toUtc). A refund counts against the day it was SENT.</summary>
    private async Task<decimal> RevenueAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        // Refunded payments still count as received - the refund is subtracted on its own day.
        var received = await db.Payments
            .Where(p => (p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.Refunded || p.Status == PaymentStatus.PartiallyRefunded)
                        && p.PaidAtUtc >= fromUtc && p.PaidAtUtc < toUtc)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;
        var refunded = await db.Refunds
            .Where(r => r.Status == RefundStatus.Completed && r.CompletedAtUtc >= fromUtc && r.CompletedAtUtc < toUtc)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0;
        return received - refunded;
    }
}
