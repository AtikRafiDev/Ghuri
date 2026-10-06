using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Booking.Queries.SearchBookings;

/// <summary>
/// The admin bookings table (17-day plan, Day 12: SearchBookings). Newest
/// first. Search: part of a booking number or the contact's name, or a
/// whole mobile number. TripFrom/TripTo filter on the trip's start date.
/// </summary>
public sealed record SearchBookingsQuery(
    string? Search,
    BookingStatus? Status,
    BookingType? Type,
    DateOnly? TripFrom,
    DateOnly? TripTo,
    int Page = 1,
    int PageSize = 20) : IQuery<Paged<AdminBookingListItemDto>>;

/// <summary>One row of the admin bookings table.</summary>
public sealed record AdminBookingListItemDto(
    string BookingNo,
    BookingType BookingType,
    BookingStatus Status,
    string? PackageTitle,
    string ContactName,
    string ContactPhone,
    DateOnly StartDate,
    DateOnly EndDate,
    int Travellers,
    decimal TotalAmount,
    decimal PaidAmount,
    string Currency,
    DateTime BookedAtUtc);

internal sealed class SearchBookingsValidator : AbstractValidator<SearchBookingsQuery>
{
    public SearchBookingsValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paged<AdminBookingListItemDto>.MaxPageSize);
        RuleFor(x => x.TripTo).GreaterThanOrEqualTo(x => x.TripFrom)
            .When(x => x.TripFrom is not null && x.TripTo is not null)
            .WithMessage("The end of the date range can't be before its start.");
    }
}

internal sealed class SearchBookingsHandler(IReadDbContext db) : IQueryHandler<SearchBookingsQuery, Paged<AdminBookingListItemDto>>
{
    public async ValueTask<Result<Paged<AdminBookingListItemDto>>> Handle(SearchBookingsQuery query, CancellationToken cancellationToken)
    {
        var rows =
            from b in db.Bookings
            join p in db.TourPackages on b.PackageId equals p.Id into packages
            from p in packages.DefaultIfEmpty()
            select new { b, PackageTitle = p == null ? null : p.Title };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            // A phone is stored normalized (PhoneNumber) - compare whole numbers, in any accepted spelling.
            if (PhoneNumber.IsValid(search))
            {
                var phone = PhoneNumber.Create(search);
                rows = rows.Where(r => r.b.ContactPhone == phone);
            }
            else
            {
                rows = rows.Where(r => r.b.BookingNo.Contains(search) || r.b.ContactName.Contains(search));
            }
        }
        if (query.Status is { } status)
            rows = rows.Where(r => r.b.Status == status);
        if (query.Type is { } type)
            rows = rows.Where(r => r.b.BookingType == type);
        if (query.TripFrom is { } from)
            rows = rows.Where(r => r.b.StartDate >= from);
        if (query.TripTo is { } to)
            rows = rows.Where(r => r.b.StartDate <= to);

        var page = await rows
            // Newest first. CreatedAtUtc is a shadow column (audit interceptor); Id last for a stable order.
            .OrderByDescending(r => EF.Property<DateTime>(r.b, "CreatedAtUtc")).ThenBy(r => r.b.Id)
            .Select(r => new
            {
                r.b.BookingNo, r.b.BookingType, r.b.Status, r.PackageTitle, r.b.ContactName, r.b.ContactPhone,
                r.b.StartDate, r.b.EndDate, Travellers = r.b.Adults + r.b.Children + r.b.Infants,
                r.b.TotalAmount, r.b.PaidAmount, r.b.Currency,
                BookedAtUtc = EF.Property<DateTime>(r.b, "CreatedAtUtc")
            })
            .ToPagedAsync(query.Page, query.PageSize, cancellationToken);

        return page.Map(r => new AdminBookingListItemDto(
            r.BookingNo, r.BookingType, r.Status, r.PackageTitle, r.ContactName, r.ContactPhone.Value,
            r.StartDate, r.EndDate, r.Travellers, r.TotalAmount, r.PaidAmount, r.Currency, r.BookedAtUtc));
    }
}
