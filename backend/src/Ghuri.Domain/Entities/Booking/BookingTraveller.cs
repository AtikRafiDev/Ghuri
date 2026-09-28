using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Booking;

/// <summary>One person travelling on a booking. Owned by Booking (internal Create - see PackageImage's remarks for why).</summary>
public sealed class BookingTraveller : BaseEntity
{
    public Guid BookingId { get; private set; }
    public TravellerType TravellerType { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public Gender? Gender { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }

    /// <summary>ISO 3166-1 alpha-2 code, e.g. "BD".</summary>
    public string? Nationality { get; private set; }

    /// <summary>Encrypted at rest by Infrastructure (international tours only) - stored here as opaque bytes; Domain never sees the plaintext.</summary>
    public byte[]? PassportNo { get; private set; }

    public string? Phone { get; private set; }
    public bool IsLead { get; private set; }

    private BookingTraveller()
    {
    }

    internal static BookingTraveller Create(
        Guid bookingId, TravellerType travellerType, string fullName, bool isLead,
        Gender? gender = null, DateOnly? dateOfBirth = null, string? nationality = null,
        byte[]? passportNo = null, string? phone = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        return new BookingTraveller
        {
            BookingId = bookingId,
            TravellerType = travellerType,
            FullName = fullName,
            IsLead = isLead,
            Gender = gender,
            DateOfBirth = dateOfBirth,
            Nationality = nationality,
            PassportNo = passportNo,
            Phone = phone
        };
    }
}
