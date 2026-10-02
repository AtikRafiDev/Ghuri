using System.Text.Json.Serialization;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Booking.Commands.CreateBooking;

/// <summary>
/// Book a trip (17-day plan, Day 8: POST /bookings). Fixed-departure package:
/// send DepartureId. Flexible stay: send StartDate and Nights. The other pair
/// is ignored - the package decides which kind of booking it is.
/// </summary>
/// <remarks>
/// The price is NOT part of the request: the server works it out (the same
/// PriceCalculator as the quote), so a browser can never set its own price.
/// The customer is whoever is logged in.
/// </remarks>
public sealed record CreateBookingCommand(
    string PackageSlug,
    Guid? DepartureId,
    DateOnly? StartDate,
    int? Nights,
    IReadOnlyList<TravellerInput> Travellers,
    string ContactName,
    string ContactPhone,
    string? ContactEmail,
    string? SpecialRequest) : ICommand<CreateBookingResponse>
{
    /// <summary>
    /// From the "Idempotency-Key" HEADER (the controller copies it in), never
    /// from the body - [JsonIgnore]. Nullable on purpose: ASP.NET treats a
    /// non-nullable string as required and would reject a missing header
    /// before the validator could give its clearer message.
    /// </summary>
    [JsonIgnore]
    public string? IdempotencyKey { get; init; }
}

/// <summary>One traveller as typed into the checkout form. Exactly one is the lead, and an adult.</summary>
public sealed record TravellerInput(
    TravellerType Type,
    string FullName,
    bool IsLead,
    Gender? Gender = null,
    DateOnly? DateOfBirth = null,
    string? Nationality = null,
    string? Phone = null);

/// <summary>
/// What the checkout needs next: the booking number to show, the total to
/// pay, and when the seat hold ends (for the countdown).
/// </summary>
public sealed record CreateBookingResponse(
    Guid BookingId,
    string BookingNo,
    decimal TotalAmount,
    string Currency,
    DateTime HoldExpiresAtUtc);
