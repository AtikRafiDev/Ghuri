using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Payments.Queries.SearchRefunds;

/// <summary>
/// The admin refunds table (17-day plan, Day 12: "refunds to process").
/// OpenOnly = money still owed (Requested, with SSLCommerz, or failed there) -
/// oldest first, so nobody waits longest; otherwise every refund, newest first.
/// </summary>
public sealed record SearchRefundsQuery(bool OpenOnly, string? Search, int Page = 1, int PageSize = 20) : IQuery<Paged<AdminRefundListItemDto>>;

/// <summary>
/// One refund, with what staff need to send the money: how the customer
/// paid (PaymentProvider - SSLCommerz can send it back itself; PaymentMethod -
/// send bKash back to bKash) and their contact. RequestedByName null = the
/// system (a late or double payment). Reference: the bKash / bank id staff
/// typed, or SSLCommerz's refund id. RejectReason: why it was rejected, or
/// why SSLCommerz refused or cancelled it.
/// </summary>
public sealed record AdminRefundListItemDto(
    string RefundNo,
    string BookingNo,
    string PaymentNo,
    PaymentProvider PaymentProvider,
    string? PaymentMethod,
    string ContactName,
    string ContactPhone,
    decimal Amount,
    decimal RefundPercent,
    string Currency,
    string Reason,
    RefundStatus Status,
    string? RequestedByName,
    DateTime RequestedAtUtc,
    string? Reference,
    DateTime? CompletedAtUtc,
    string? RejectReason);

internal sealed class SearchRefundsValidator : AbstractValidator<SearchRefundsQuery>
{
    public SearchRefundsValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paged<AdminRefundListItemDto>.MaxPageSize);
    }
}

internal sealed class SearchRefundsHandler(IReadDbContext db) : IQueryHandler<SearchRefundsQuery, Paged<AdminRefundListItemDto>>
{
    public async ValueTask<Result<Paged<AdminRefundListItemDto>>> Handle(SearchRefundsQuery query, CancellationToken cancellationToken)
    {
        var rows =
            from r in db.Refunds
            join b in db.Bookings on r.BookingId equals b.Id
            join p in db.Payments on r.PaymentId equals p.Id
            join u in db.Users on r.RequestedBy equals (Guid?)u.Id into users
            from u in users.DefaultIfEmpty()
            select new { r, b, p, RequestedByName = u == null ? null : u.FullName, RequestedAtUtc = EF.Property<DateTime>(r, "CreatedAtUtc") };

        if (query.OpenOnly)
            rows = rows.Where(x => x.r.Status == RefundStatus.Requested || x.r.Status == RefundStatus.Approved
                                   || x.r.Status == RefundStatus.Processing || x.r.Status == RefundStatus.Failed); // = Refund.IsOpen
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(x => x.r.RefundNo.Contains(search) || x.b.BookingNo.Contains(search) || x.b.ContactName.Contains(search));
        }

        rows = query.OpenOnly
            ? rows.OrderBy(x => x.RequestedAtUtc).ThenBy(x => x.r.Id)
            : rows.OrderByDescending(x => x.RequestedAtUtc).ThenBy(x => x.r.Id);

        var page = await rows
            .Select(x => new
            {
                x.r.RefundNo, x.b.BookingNo, x.p.PaymentNo, PaymentProvider = x.p.Provider, PaymentMethod = x.p.Method, x.b.ContactName, x.b.ContactPhone,
                x.r.Amount, x.r.RefundPercent, x.p.Currency, x.r.Reason, x.r.Status, x.RequestedByName, x.RequestedAtUtc,
                Reference = x.r.ProviderRefundId, x.r.CompletedAtUtc, RejectReason = x.r.FailureReason
            })
            .ToPagedAsync(query.Page, query.PageSize, cancellationToken);

        return page.Map(x => new AdminRefundListItemDto(
            x.RefundNo, x.BookingNo, x.PaymentNo, x.PaymentProvider, x.PaymentMethod, x.ContactName, x.ContactPhone.Value,
            x.Amount, x.RefundPercent, x.Currency, x.Reason, x.Status, x.RequestedByName, x.RequestedAtUtc,
            x.Reference, x.CompletedAtUtc, x.RejectReason));
    }
}
