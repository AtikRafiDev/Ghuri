using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Payments.Queries.SearchPayments;

/// <summary>
/// The admin payments table (17-day plan, Day 12: SearchPayments) - every
/// attempt, online and manual, newest first. Search: payment number, booking
/// number or the bank / gateway transaction id. From/To: the day it started
/// (Bangladesh dates).
/// </summary>
public sealed record SearchPaymentsQuery(
    string? Search,
    PaymentStatus? Status,
    PaymentProvider? Provider,
    DateOnly? From,
    DateOnly? To,
    int Page = 1,
    int PageSize = 20) : IQuery<Paged<AdminPaymentListItemDto>>;

/// <summary>Reference: the gateway's / bank's transaction id (SSLCommerz bank_tran_id, or what staff typed).</summary>
public sealed record AdminPaymentListItemDto(
    string PaymentNo,
    string BookingNo,
    PaymentProvider Provider,
    string? Method,
    PaymentStatus Status,
    decimal Amount,
    string Currency,
    DateTime InitiatedAtUtc,
    DateTime? PaidAtUtc,
    string? Reference,
    string? FailureReason);

internal sealed class SearchPaymentsValidator : AbstractValidator<SearchPaymentsQuery>
{
    public SearchPaymentsValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paged<AdminPaymentListItemDto>.MaxPageSize);
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("The end of the date range can't be before its start.");
    }
}

internal sealed class SearchPaymentsHandler(IReadDbContext db) : IQueryHandler<SearchPaymentsQuery, Paged<AdminPaymentListItemDto>>
{
    public async ValueTask<Result<Paged<AdminPaymentListItemDto>>> Handle(SearchPaymentsQuery query, CancellationToken cancellationToken)
    {
        var rows =
            from p in db.Payments
            join b in db.Bookings on p.BookingId equals b.Id
            select new { p, b.BookingNo };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(r => r.p.PaymentNo.Contains(search) || r.BookingNo.Contains(search) || r.p.ProviderTransactionId == search);
        }
        if (query.Status is { } status)
            rows = rows.Where(r => r.p.Status == status);
        if (query.Provider is { } provider)
            rows = rows.Where(r => r.p.Provider == provider);
        // A Bangladesh day is [00:00, 24:00) at UTC+6.
        if (query.From is { } from)
        {
            var fromUtc = AgencyDay.StartUtc(from);
            rows = rows.Where(r => r.p.InitiatedAtUtc >= fromUtc);
        }
        if (query.To is { } to)
        {
            var toUtc = AgencyDay.StartUtc(to.AddDays(1));
            rows = rows.Where(r => r.p.InitiatedAtUtc < toUtc);
        }

        var page = await rows
            .OrderByDescending(r => r.p.InitiatedAtUtc).ThenBy(r => r.p.Id)
            .Select(r => new AdminPaymentListItemDto(
                r.p.PaymentNo, r.BookingNo, r.p.Provider, r.p.Method, r.p.Status, r.p.Amount, r.p.Currency,
                r.p.InitiatedAtUtc, r.p.PaidAtUtc, r.p.ProviderTransactionId, r.p.FailureReason))
            .ToPagedAsync(query.Page, query.PageSize, cancellationToken);

        return page;
    }
}
