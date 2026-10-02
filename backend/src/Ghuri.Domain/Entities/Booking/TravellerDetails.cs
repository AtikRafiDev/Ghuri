using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Booking;

/// <summary>
/// One person as the checkout form sends them - the input for
/// Booking.CreateForDeparture / CreateFlexible. Same idea as
/// TourPackageDetails: a plain bundle of fields, checked by Booking.
/// </summary>
/// <remarks>
/// No passport number yet: it must be encrypted at rest, which arrives with
/// international tours. The column (BookingTravellers.PassportNo) is ready.
/// </remarks>
public sealed record TravellerDetails(
    TravellerType Type,
    string FullName,
    bool IsLead,
    Gender? Gender = null,
    DateOnly? DateOfBirth = null,
    string? Nationality = null,
    string? Phone = null);

/// <summary>Who the agency calls or emails about this booking - usually the lead traveller, but not always (a parent booking for their children).</summary>
public sealed record BookingContact(string Name, PhoneNumber Phone, string? Email = null);
