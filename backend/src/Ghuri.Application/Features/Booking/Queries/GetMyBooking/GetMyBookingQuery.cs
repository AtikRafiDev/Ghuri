using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Booking.Queries.GetMyBooking;

/// <summary>
/// One of the logged-in customer's own bookings, by its number (TB100001) -
/// the checkout reloads it after a refresh, and "My bookings" (Day 11)
/// shows it. Someone else's booking is "not found", never "forbidden":
/// a stranger can't even learn that a number exists.
/// </summary>
public sealed record GetMyBookingQuery(string BookingNo) : IQuery<MyBookingDto>;

/// <summary>
/// The booking as the customer sees it. Prices are the snapshot taken when
/// booking - later price changes on the package never change it.
/// HoldExpiresAtUtc is only set while Status is PendingPayment (the countdown).
/// </summary>
public sealed record MyBookingDto(
    Guid Id,
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
    string Currency,
    DateTime? HoldExpiresAtUtc,
    string ContactName,
    string ContactPhone,
    string? ContactEmail,
    string? SpecialRequest,
    IReadOnlyList<MyBookingTravellerDto> Travellers);

public sealed record MyBookingTravellerDto(string FullName, TravellerType Type, bool IsLead);
