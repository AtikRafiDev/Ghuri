using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Booking;

/// <summary>
/// A customer's reservation (blueprint: booking.Bookings [A]) - a seat on
/// a fixed departure, a flexible stay, or (Day 15) an accepted custom-trip
/// quote. The aggregate root for its own travellers and add-ons.
/// </summary>
/// <remarks>
/// <para>
/// BookingType decides which link must be set: FixedDeparture → DepartureId
/// + PackageId · FlexibleStay → PackageId only · CustomTrip → CustomTripId
/// only. All of it arrives as one BookingStay, which can only be built in a
/// valid shape; the database's CK_Bookings_Shape checks the same rule.
/// </para>
/// <para>
/// StartDate / EndDate / Nights are stored for EVERY type - copied from the
/// departure for a fixed trip - so "upcoming trips", the voucher and the
/// cancellation policy ("days before the start") work the same for all
/// three without joining anywhere.
/// </para>
/// <para>
/// Create accepts prices already computed by the caller (PriceCalculator).
/// The friendly factories (CreateForDeparture / CreateFlexible) and the
/// state machine (Expire, Cancel, Confirm) are Day 8 Part 2.
/// </para>
/// </remarks>
public sealed class Booking : AggregateRoot, IAuditable
{
    /// <summary>Human-readable code like TB100001, generated from a SQL SEQUENCE (see AppDbContext).</summary>
    public string BookingNo { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }

    public BookingType BookingType { get; private set; }

    /// <summary>Only for a FixedDeparture booking.</summary>
    public Guid? DepartureId { get; private set; }

    /// <summary>Fixed and flexible bookings (for a fixed one, denormalized from the departure so reports don't need to join through Departures). Null for a custom trip, which has no package.</summary>
    public Guid? PackageId { get; private set; }

    /// <summary>Only for a CustomTrip booking. No foreign key yet: booking.CustomTrips arrives on Day 13.</summary>
    public Guid? CustomTripId { get; private set; }

    /// <summary>The first day of the trip (a flexible stay's check-in day).</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>The last day of a fixed trip, or a flexible stay's check-out day (StartDate + Nights).</summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>Nights away - 0 is possible for a fixed day trip; a flexible stay always has at least 1.</summary>
    public byte Nights { get; private set; }

    public byte Adults { get; private set; }
    public byte Children { get; private set; }
    public byte Infants { get; private set; }

    public decimal AdultPriceSnapshot { get; private set; }
    public decimal ChildPriceSnapshot { get; private set; }
    public decimal InfantPriceSnapshot { get; private set; }

    public decimal SubTotal { get; private set; }
    public decimal AddOnTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public string Currency { get; private set; } = "BDT";

    public PaymentPlan PaymentPlan { get; private set; }
    public DateOnly? BalanceDueDate { get; private set; }

    /// <summary>FK to marketing.Coupons - not configured yet, the marketing schema hasn't been built.</summary>
    public Guid? CouponId { get; private set; }

    public BookingStatus Status { get; private set; }

    /// <summary>Only set while Status is PendingPayment - the seat hold deadline.</summary>
    public DateTime? HoldExpiresAtUtc { get; private set; }

    public string ContactName { get; private set; } = string.Empty;
    public PhoneNumber ContactPhone { get; private set; } = null!;
    public string? ContactEmail { get; private set; }
    public string? SpecialRequest { get; private set; }
    public BookingSource Source { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelReason { get; private set; }

    private readonly List<BookingTraveller> _travellers = [];
    public IReadOnlyList<BookingTraveller> Travellers => _travellers.AsReadOnly();

    private readonly List<BookingAddOn> _addOns = [];
    public IReadOnlyList<BookingAddOn> AddOns => _addOns.AsReadOnly();

    private readonly List<BookingStatusHistory> _history = [];

    /// <summary>Every status transition this booking has gone through - written to on every state change, read back for audit/support screens.</summary>
    public IReadOnlyList<BookingStatusHistory> History => _history.AsReadOnly();

    private Booking()
    {
    }

    public static Booking Create(
        string bookingNo, Guid customerId, BookingStay stay,
        byte adults, byte children, byte infants,
        decimal adultPriceSnapshot, decimal childPriceSnapshot, decimal infantPriceSnapshot,
        decimal subTotal, decimal addOnTotal, decimal discountAmount, string currency,
        PaymentPlan paymentPlan, string contactName, PhoneNumber contactPhone,
        BookingSource source, DateTime nowUtc, DateTime holdExpiresAtUtc,
        string? contactEmail = null, string? specialRequest = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingNo);
        ArgumentNullException.ThrowIfNull(stay);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactName);
        if (adults < 1)
            throw new ArgumentOutOfRangeException(nameof(adults), "A booking needs at least one adult.");

        var totalAmount = subTotal + addOnTotal - discountAmount;

        var booking = new Booking
        {
            BookingNo = bookingNo,
            CustomerId = customerId,
            BookingType = stay.Type,
            DepartureId = stay.DepartureId,
            PackageId = stay.PackageId,
            CustomTripId = stay.CustomTripId,
            StartDate = stay.StartDate,
            EndDate = stay.EndDate,
            Nights = stay.Nights,
            Adults = adults,
            Children = children,
            Infants = infants,
            AdultPriceSnapshot = adultPriceSnapshot,
            ChildPriceSnapshot = childPriceSnapshot,
            InfantPriceSnapshot = infantPriceSnapshot,
            SubTotal = subTotal,
            AddOnTotal = addOnTotal,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            PaidAmount = 0,
            Currency = currency,
            PaymentPlan = paymentPlan,
            ContactName = contactName,
            ContactPhone = contactPhone,
            ContactEmail = contactEmail,
            SpecialRequest = specialRequest,
            Source = source,
            Status = BookingStatus.PendingPayment,
            HoldExpiresAtUtc = holdExpiresAtUtc
        };

        booking._history.Add(BookingStatusHistory.Record(booking.Id, null, BookingStatus.PendingPayment, nowUtc));
        return booking;
    }

    public BookingTraveller AddTraveller(
        TravellerType travellerType, string fullName, bool isLead,
        Gender? gender = null, DateOnly? dateOfBirth = null, string? nationality = null,
        byte[]? passportNo = null, string? phone = null)
    {
        var traveller = BookingTraveller.Create(Id, travellerType, fullName, isLead, gender, dateOfBirth, nationality, passportNo, phone);
        _travellers.Add(traveller);
        return traveller;
    }

    public BookingAddOn AddAddOn(Guid addOnId, string nameSnapshot, decimal unitPrice, short quantity)
    {
        var addOn = BookingAddOn.Create(Id, addOnId, nameSnapshot, unitPrice, quantity);
        _addOns.Add(addOn);
        return addOn;
    }
}
