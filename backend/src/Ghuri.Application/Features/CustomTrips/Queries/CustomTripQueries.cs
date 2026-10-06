using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.CustomTrips.Queries;

// The four reads of the custom-trips feature (17-day plan, Day 13):
// GetMyCustomTrips, GetMyCustomTrip, GetCustomTripQueue, GetCustomTripForAdmin.

/// <summary>One line of a trip list - "CT1001 · Cox's Bazar → Sylhet · 20–25 Dec · Quoted ৳62,000".</summary>
public sealed record CustomTripListItemDto(
    string TripNo,
    CustomTripStatus Status,
    string Route,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalNights,
    int People,
    string ContactName,
    string ContactPhone,
    DateTime SubmittedAtUtc,
    decimal? QuoteTotal,
    DateTime? QuoteExpiresAtUtc);

// ---------- Customer ----------

/// <summary>The logged-in customer's trip requests, newest first ("My trips").</summary>
public sealed record GetMyCustomTripsQuery : IQuery<IReadOnlyList<CustomTripListItemDto>>;

internal sealed class GetMyCustomTripsHandler(IReadDbContext db, ICurrentUser currentUser, CustomTripReader reader)
    : IQueryHandler<GetMyCustomTripsQuery, IReadOnlyList<CustomTripListItemDto>>
{
    public async ValueTask<Result<IReadOnlyList<CustomTripListItemDto>>> Handle(GetMyCustomTripsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        var rows = await db.CustomTrips
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.SubmittedAtUtc)
            .Select(t => new
            {
                t.Id, t.TripNo, t.Status, t.StartDate, t.EndDate, t.TotalNights, People = t.Adults + t.Children + t.Infants,
                t.ContactName, t.ContactPhone, t.SubmittedAtUtc, t.QuoteTotal, t.QuoteExpiresAtUtc
            })
            .ToListAsync(cancellationToken);
        var routes = await reader.RoutesAsync(rows.Select(r => r.Id).ToList(), cancellationToken);

        return rows.Select(r => new CustomTripListItemDto(
                r.TripNo, r.Status, routes.GetValueOrDefault(r.Id, ""), r.StartDate, r.EndDate, r.TotalNights, r.People,
                r.ContactName, r.ContactPhone.Value, r.SubmittedAtUtc, r.QuoteTotal, r.QuoteExpiresAtUtc))
            .ToList();
    }
}

/// <summary>One of the customer's own trips. Someone else's is "not found".</summary>
public sealed record GetMyCustomTripQuery(string TripNo) : IQuery<CustomTripDto>;

internal sealed class GetMyCustomTripHandler(ICurrentUser currentUser, CustomTripReader reader) : IQueryHandler<GetMyCustomTripQuery, CustomTripDto>
{
    public async ValueTask<Result<CustomTripDto>> Handle(GetMyCustomTripQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        var loaded = await reader.LoadAsync(query.TripNo, customerId, cancellationToken);
        if (loaded is null)
            return CustomTripErrors.TripNotFound;
        return loaded.Value.Trip;
    }
}

// ---------- Staff ----------

/// <summary>
/// The staff queue. Waiting requests (Status = Submitted, the default view)
/// come OLDEST first - the plan's target is a quote within 24 hours; every
/// other view newest first. Search: trip number, name or mobile.
/// </summary>
public sealed record GetCustomTripQueueQuery(CustomTripStatus? Status, string? Search, int Page = 1, int PageSize = 20)
    : IQuery<Paged<CustomTripListItemDto>>;

internal sealed class GetCustomTripQueueValidator : AbstractValidator<GetCustomTripQueueQuery>
{
    public GetCustomTripQueueValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paged<CustomTripListItemDto>.MaxPageSize);
    }
}

internal sealed class GetCustomTripQueueHandler(IReadDbContext db, CustomTripReader reader)
    : IQueryHandler<GetCustomTripQueueQuery, Paged<CustomTripListItemDto>>
{
    public async ValueTask<Result<Paged<CustomTripListItemDto>>> Handle(GetCustomTripQueueQuery query, CancellationToken cancellationToken)
    {
        var trips = db.CustomTrips.AsQueryable();
        if (query.Status is { } status)
            trips = trips.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (Domain.ValueObjects.PhoneNumber.TryCreate(search, out var phone) && phone is not null)
                trips = trips.Where(t => t.ContactPhone == phone);
            else
                trips = trips.Where(t => t.TripNo.Contains(search) || t.ContactName.Contains(search));
        }

        trips = query.Status == CustomTripStatus.Submitted
            ? trips.OrderBy(t => t.SubmittedAtUtc).ThenBy(t => t.Id)
            : trips.OrderByDescending(t => t.SubmittedAtUtc).ThenBy(t => t.Id);

        var page = await trips
            .Select(t => new
            {
                t.Id, t.TripNo, t.Status, t.StartDate, t.EndDate, t.TotalNights, People = t.Adults + t.Children + t.Infants,
                t.ContactName, t.ContactPhone, t.SubmittedAtUtc, t.QuoteTotal, t.QuoteExpiresAtUtc
            })
            .ToPagedAsync(query.Page, query.PageSize, cancellationToken);
        var routes = await reader.RoutesAsync(page.Items.Select(r => r.Id).ToList(), cancellationToken);

        return page.Map(r => new CustomTripListItemDto(
            r.TripNo, r.Status, routes.GetValueOrDefault(r.Id, ""), r.StartDate, r.EndDate, r.TotalNights, r.People,
            r.ContactName, r.ContactPhone.Value, r.SubmittedAtUtc, r.QuoteTotal, r.QuoteExpiresAtUtc));
    }
}

/// <summary>One trip for staff: the trip, the account behind it, who quoted it, and how often they've booked before.</summary>
public sealed record GetCustomTripForAdminQuery(string TripNo) : IQuery<AdminCustomTripDto>;

public sealed record AdminCustomTripDto(CustomTripDto Trip, AdminCustomTripCustomerDto Customer, string? QuotedByName, int PreviousBookings);

public sealed record AdminCustomTripCustomerDto(Guid Id, string FullName, string Phone, string? Email);

internal sealed class GetCustomTripForAdminHandler(IReadDbContext db, CustomTripReader reader)
    : IQueryHandler<GetCustomTripForAdminQuery, AdminCustomTripDto>
{
    public async ValueTask<Result<AdminCustomTripDto>> Handle(GetCustomTripForAdminQuery query, CancellationToken cancellationToken)
    {
        var loaded = await reader.LoadAsync(query.TripNo, customerId: null, cancellationToken);
        if (loaded is null)
            return CustomTripErrors.TripNotFound;
        var (_, customerId, quotedBy, trip) = loaded.Value;

        var customer = await db.Users
            .Where(u => u.Id == customerId)
            .Select(u => new { u.Id, u.FullName, u.PhoneNumber, u.Email })
            .SingleAsync(cancellationToken);
        var quotedByName = quotedBy is { } staffId
            ? await db.Users.Where(u => u.Id == staffId).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken)
            : null;
        var previousBookings = await db.Bookings.CountAsync(
            b => b.CustomerId == customerId && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed),
            cancellationToken);

        return new AdminCustomTripDto(
            trip, new AdminCustomTripCustomerDto(customer.Id, customer.FullName, customer.PhoneNumber.Value, customer.Email),
            quotedByName, previousBookings);
    }
}
