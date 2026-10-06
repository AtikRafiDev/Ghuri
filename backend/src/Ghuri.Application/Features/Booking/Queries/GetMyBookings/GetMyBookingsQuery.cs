using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking.Queries.GetMyBookings;

/// <summary>
/// The logged-in customer's bookings, newest first ("My bookings" -
/// 17-day plan, Day 11: GetMyBookings). Not paged: one customer has a
/// handful of trips, not thousands. Expired holds are included - the
/// customer may wonder where that booking went.
/// </summary>
public sealed record GetMyBookingsQuery : IQuery<IReadOnlyList<MyBookingSummaryDto>>;

/// <summary>One line of "My bookings". The details (travellers, prices, refund) are GetMyBooking.</summary>
public sealed record MyBookingSummaryDto(
    string BookingNo,
    BookingType BookingType,
    BookingStatus Status,
    string? PackageTitle,
    string? PackageSlug,
    DateOnly StartDate,
    DateOnly EndDate,
    int Nights,
    int Travellers,
    decimal TotalAmount,
    string Currency,
    DateTime? HoldExpiresAtUtc);

internal sealed class GetMyBookingsHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyBookingsQuery, IReadOnlyList<MyBookingSummaryDto>>
{
    public async ValueTask<Result<IReadOnlyList<MyBookingSummaryDto>>> Handle(GetMyBookingsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        // BookingNo comes from a sequence, so a higher number = booked later.
        var rows = await (
                from b in db.Bookings
                where b.CustomerId == customerId
                join p in db.TourPackages on b.PackageId equals p.Id into packages
                from p in packages.DefaultIfEmpty()
                orderby b.BookingNo descending
                select new { Booking = b, PackageTitle = p == null ? null : p.Title, PackageSlug = p == null ? null : p.Slug })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new MyBookingSummaryDto(
                r.Booking.BookingNo, r.Booking.BookingType, r.Booking.Status,
                r.PackageTitle, r.PackageSlug?.Value,
                r.Booking.StartDate, r.Booking.EndDate, r.Booking.Nights,
                r.Booking.Adults + r.Booking.Children + r.Booking.Infants,
                r.Booking.TotalAmount, r.Booking.Currency, r.Booking.HoldExpiresAtUtc))
            .ToList();
    }
}
