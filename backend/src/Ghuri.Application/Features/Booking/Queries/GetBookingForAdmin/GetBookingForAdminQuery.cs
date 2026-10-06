using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking.Queries.GetBookingForAdmin;

/// <summary>
/// Everything staff need about one booking (17-day plan, Day 12:
/// GetBookingForAdmin): the trip, the people, every status change, every
/// payment attempt and every refund - plus what can be done with it now.
/// </summary>
public sealed record GetBookingForAdminQuery(string BookingNo) : IQuery<AdminBookingDto>;

/// <summary>
/// AmountDue: what a manual payment must be to settle it (0 when paid).
/// CanCancel / CanRecordPayment: whether those buttons apply right now.
/// </summary>
public sealed record AdminBookingDto(
    string BookingNo,
    BookingType BookingType,
    BookingStatus Status,
    string? PackageTitle,
    string? PackageSlug,
    DateOnly StartDate,
    DateOnly EndDate,
    int Nights,
    int Adults,
    int Children,
    int Infants,
    decimal AdultPrice,
    decimal ChildPrice,
    decimal InfantPrice,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal AmountDue,
    string Currency,
    DateTime? HoldExpiresAtUtc,
    DateTime BookedAtUtc,
    AdminBookingCustomerDto Customer,
    string ContactName,
    string ContactPhone,
    string? ContactEmail,
    string? SpecialRequest,
    string? CancelReason,
    DateTime? CancelledAtUtc,
    bool CanCancel,
    bool CanRecordPayment,
    IReadOnlyList<AdminBookingTravellerDto> Travellers,
    IReadOnlyList<AdminBookingHistoryDto> History,
    IReadOnlyList<AdminBookingPaymentDto> Payments,
    IReadOnlyList<AdminBookingRefundDto> Refunds);

/// <summary>The account that made the booking (the contact can be someone else).</summary>
public sealed record AdminBookingCustomerDto(Guid Id, string FullName, string Phone, string? Email);

public sealed record AdminBookingTravellerDto(string FullName, TravellerType Type, bool IsLead, string? Phone);

/// <summary>ChangedByName: null = the system (the expiry job, a payment confirmation).</summary>
public sealed record AdminBookingHistoryDto(BookingStatus? From, BookingStatus To, DateTime ChangedAtUtc, string? ChangedByName, string? Note);

public sealed record AdminBookingPaymentDto(
    string PaymentNo,
    PaymentProvider Provider,
    string? Method,
    PaymentStatus Status,
    decimal Amount,
    DateTime InitiatedAtUtc,
    DateTime? PaidAtUtc,
    string? Reference,
    string? FailureReason);

/// <summary>RequestedByName: null = the system (a late or double payment).</summary>
public sealed record AdminBookingRefundDto(
    string RefundNo,
    string PaymentNo,
    decimal Amount,
    decimal RefundPercent,
    RefundStatus Status,
    string Reason,
    string? RequestedByName,
    DateTime RequestedAtUtc,
    string? Reference,
    DateTime? CompletedAtUtc,
    string? RejectReason);

internal sealed class GetBookingForAdminHandler(IReadDbContext db) : IQueryHandler<GetBookingForAdminQuery, AdminBookingDto>
{
    public async ValueTask<Result<AdminBookingDto>> Handle(GetBookingForAdminQuery query, CancellationToken cancellationToken)
    {
        var row = await (
                from booking in db.Bookings
                where booking.BookingNo == query.BookingNo
                join c in db.Users on booking.CustomerId equals c.Id
                join p in db.TourPackages on booking.PackageId equals p.Id into packages
                from p in packages.DefaultIfEmpty()
                select new
                {
                    Booking = booking,
                    BookedAtUtc = EF.Property<DateTime>(booking, "CreatedAtUtc"),
                    Customer = new { c.Id, c.FullName, c.PhoneNumber, c.Email },
                    PackageTitle = p == null ? null : p.Title,
                    PackageSlug = p == null ? null : p.Slug
                })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return BookingErrors.BookingNotFound;

        var b = row.Booking;
        var travellers = await db.BookingTravellers
            .Where(t => t.BookingId == b.Id)
            .OrderByDescending(t => t.IsLead)
            .Select(t => new AdminBookingTravellerDto(t.FullName, t.TravellerType, t.IsLead, t.Phone))
            .ToListAsync(cancellationToken);

        var history = await (
                from h in db.BookingStatusHistory
                where h.BookingId == b.Id
                join u in db.Users on h.ChangedBy equals (Guid?)u.Id into users
                from u in users.DefaultIfEmpty()
                orderby h.Id
                select new AdminBookingHistoryDto(h.FromStatus, h.ToStatus, h.ChangedAtUtc, u == null ? null : u.FullName, h.Note))
            .ToListAsync(cancellationToken);

        var payments = await db.Payments
            .Where(p => p.BookingId == b.Id)
            .OrderBy(p => p.InitiatedAtUtc)
            .Select(p => new AdminBookingPaymentDto(
                p.PaymentNo, p.Provider, p.Method, p.Status, p.Amount, p.InitiatedAtUtc, p.PaidAtUtc, p.ProviderTransactionId, p.FailureReason))
            .ToListAsync(cancellationToken);

        var refunds = await (
                from r in db.Refunds
                where r.BookingId == b.Id
                join p in db.Payments on r.PaymentId equals p.Id
                join u in db.Users on r.RequestedBy equals (Guid?)u.Id into users
                from u in users.DefaultIfEmpty()
                orderby r.RefundNo.Length, r.RefundNo // RF999 before RF1000
                select new AdminBookingRefundDto(
                    r.RefundNo, p.PaymentNo, r.Amount, r.RefundPercent, r.Status, r.Reason, u == null ? null : u.FullName,
                    EF.Property<DateTime>(r, "CreatedAtUtc"), r.ProviderRefundId, r.CompletedAtUtc, r.FailureReason))
            .ToListAsync(cancellationToken);

        var amountDue = Math.Max(0, b.TotalAmount - b.PaidAmount);
        var canCancel = b.Status is BookingStatus.PendingPayment or BookingStatus.Confirmed;
        var canRecordPayment = amountDue > 0 && (b.Status is BookingStatus.PendingPayment or BookingStatus.Expired);

        return new AdminBookingDto(
            b.BookingNo, b.BookingType, b.Status, row.PackageTitle, row.PackageSlug?.Value,
            b.StartDate, b.EndDate, b.Nights, b.Adults, b.Children, b.Infants,
            b.AdultPriceSnapshot, b.ChildPriceSnapshot, b.InfantPriceSnapshot,
            b.TotalAmount, b.PaidAmount, amountDue, b.Currency, b.HoldExpiresAtUtc, row.BookedAtUtc,
            new AdminBookingCustomerDto(row.Customer.Id, row.Customer.FullName, row.Customer.PhoneNumber.Value, row.Customer.Email),
            b.ContactName, b.ContactPhone.Value, b.ContactEmail, b.SpecialRequest, b.CancelReason, b.CancelledAtUtc,
            canCancel, canRecordPayment,
            travellers, history, payments, refunds);
    }
}
