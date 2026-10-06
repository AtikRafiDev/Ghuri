using Ghuri.Domain.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.Services;
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
/// A booking is born through CreateForDeparture or CreateFlexible. Both work
/// the price out THEMSELVES with PriceCalculator - the same code as the
/// quote - and re-check that the trip can be booked, so no caller can save
/// a wrong price or an unbookable date. Seats are NOT reserved here: that's
/// the repository's atomic UPDATE (TryReserveSeatsAsync), done by the
/// handler in the same transaction.
/// </para>
/// <para>
/// State machine (blueprint 6.3): PendingPayment → Confirmed (paid, Day 10)
/// · PendingPayment → Expired (payment window over) · Expired → Confirmed
/// (paid late, and the seats could be taken again) · PendingPayment or
/// Confirmed → Cancelled. Every change adds a BookingStatusHistory row.
/// Completed and PartiallyPaid come later (after the trip / Phase 2).
/// </para>
/// <para>
/// Confirmed means PAID: both ways in refuse unless PaidAmount covers
/// TotalAmount, so record the money first (RecordPayment), then confirm.
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

    /// <summary>How long a new booking holds its seats while the customer pays (17-day plan: "20-minute payment window").</summary>
    public static readonly TimeSpan PaymentWindow = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Seats this booking took on its departure - adults + children (infants
    /// sit on a lap); 0 for a flexible stay or custom trip. Whoever expires or
    /// cancels the booking gives these back (IDepartureRepository.ReleaseSeatsAsync).
    /// </summary>
    public short SeatsHeld => BookingType == BookingType.FixedDeparture ? (short)(Adults + Children) : (short)0;

    /// <summary>
    /// Can a payment start now? Still PendingPayment AND inside the hold. A
    /// hold that has just run out counts as over even before the expiry job
    /// has marked it Expired - a customer must never start paying for seats
    /// that are about to be given away.
    /// </summary>
    public bool IsAwaitingPayment(DateTime nowUtc) =>
        Status == BookingStatus.PendingPayment && HoldExpiresAtUtc is { } deadline && nowUtc < deadline;

    private Booking()
    {
    }

    /// <summary>
    /// A booking on a fixed departure, priced from the departure's own
    /// prices. The handler must reserve the seats (TryReserveSeatsAsync) in
    /// the same transaction - this only checks there are enough right now.
    /// </summary>
    /// <exception cref="DomainException">The package isn't for sale, or the date can't take this many people.</exception>
    public static Booking CreateForDeparture(
        string bookingNo, Guid customerId, TourPackage package, Departure departure,
        IReadOnlyList<TravellerDetails> travellers, BookingContact contact, string? specialRequest,
        BookingSource source, DateOnly today, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(departure);
        EnsureForSale(package);
        var counts = CountTravellers(travellers);

        // The handler checked this already, to answer with a friendly
        // message; checking again here means no caller can skip it.
        if (departure.CheckBookable(today, counts.Seats) != DepartureBookability.Bookable)
            throw new DomainException("departure_not_bookable", "This date can't take this booking.");

        var price = PriceCalculator.ForDeparture(package, departure, counts, singleRooms: 0);
        var stay = BookingStay.ForDeparture(departure.Id, package.Id, departure.StartDate, departure.EndDate, package.DurationNights);

        return Create(bookingNo, customerId, stay, counts,
            departure.AdultPrice, departure.ChildPrice, departure.InfantPrice, price,
            travellers, contact, specialRequest, source, nowUtc);
    }

    /// <summary>A flexible stay: the customer's own check-in day and nights, priced per person per stay.</summary>
    /// <exception cref="DomainException">The package isn't for sale, or the nights / start date aren't allowed.</exception>
    public static Booking CreateFlexible(
        string bookingNo, Guid customerId, TourPackage package, DateOnly checkIn, byte nights,
        IReadOnlyList<TravellerDetails> travellers, BookingContact contact, string? specialRequest,
        BookingSource source, DateOnly today, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(package);
        EnsureForSale(package);
        var counts = CountTravellers(travellers);

        if (package.CheckFlexibleStay(today, checkIn, nights) != FlexibleStayBookability.Bookable)
            throw new DomainException("stay_not_bookable", "These dates can't be booked for this package.");

        var price = PriceCalculator.ForFlexibleStay(package, nights, counts);
        // Children pay the adult rate, infants are free - the same rules as PriceCalculator.
        var perPerson = PriceCalculator.PerPersonForNights(package, nights);

        return Create(bookingNo, customerId, BookingStay.Flexible(package.Id, checkIn, nights), counts,
            perPerson, perPerson, 0, price,
            travellers, contact, specialRequest, source, nowUtc);
    }

    private static Booking Create(
        string bookingNo, Guid customerId, BookingStay stay, Travellers counts,
        decimal adultPrice, decimal childPrice, decimal infantPrice, PriceBreakdown price,
        IReadOnlyList<TravellerDetails> travellers, BookingContact contact, string? specialRequest,
        BookingSource source, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingNo);
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentException.ThrowIfNullOrWhiteSpace(contact.Name);
        ArgumentNullException.ThrowIfNull(contact.Phone);

        // The per-person snapshot must explain the total exactly. A price line
        // it has no column for (a single-room supplement - not offered on a
        // booking yet) would make the invoice disagree with what is charged.
        if (price.Total != counts.Adults * adultPrice + counts.Children * childPrice + counts.Infants * infantPrice)
            throw new InvalidOperationException("The price has a line the booking can't store.");

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
            Adults = (byte)counts.Adults,
            Children = (byte)counts.Children,
            Infants = (byte)counts.Infants,
            AdultPriceSnapshot = adultPrice,
            ChildPriceSnapshot = childPrice,
            InfantPriceSnapshot = infantPrice,
            SubTotal = price.Total,
            AddOnTotal = 0,
            DiscountAmount = 0,
            TotalAmount = price.Total,
            PaidAmount = 0,
            Currency = price.Currency,
            PaymentPlan = PaymentPlan.Full, // partial/advance payment is Phase 2
            ContactName = contact.Name.Trim(),
            ContactPhone = contact.Phone,
            ContactEmail = string.IsNullOrWhiteSpace(contact.Email) ? null : contact.Email.Trim(),
            SpecialRequest = string.IsNullOrWhiteSpace(specialRequest) ? null : specialRequest.Trim(),
            Source = source,
            Status = BookingStatus.PendingPayment,
            HoldExpiresAtUtc = nowUtc + PaymentWindow
        };

        booking._history.Add(BookingStatusHistory.Record(booking.Id, null, BookingStatus.PendingPayment, nowUtc));

        foreach (var t in travellers)
        {
            booking._travellers.Add(BookingTraveller.Create(
                booking.Id, t.Type, t.FullName.Trim(), t.IsLead, t.Gender, t.DateOfBirth,
                string.IsNullOrWhiteSpace(t.Nationality) ? null : t.Nationality.Trim().ToUpperInvariant(),
                passportNo: null,
                string.IsNullOrWhiteSpace(t.Phone) ? null : t.Phone.Trim()));
        }

        return booking;
    }

    // ---------- State machine ----------

    /// <summary>
    /// The payment window ended without payment (the expiry job). The handler
    /// then releases SeatsHeld. Refused while the window is still open.
    /// </summary>
    public void Expire(DateTime nowUtc)
    {
        if (Status != BookingStatus.PendingPayment)
            throw new DomainException("booking_not_pending", "Only a booking waiting for payment can expire.");
        if (nowUtc < HoldExpiresAtUtc)
            throw new DomainException("booking_hold_open", "The payment window is still open.");

        ChangeStatus(BookingStatus.Expired, nowUtc, changedBy: null, "Payment window ended.");
    }

    /// <summary>True once the money received covers the price.</summary>
    public bool IsPaidInFull => PaidAmount >= TotalAmount;

    /// <summary>
    /// Money arrived for this booking (a payment the gateway confirmed). Kept
    /// whatever the status: money received is a fact. A payment for an
    /// expired or cancelled booking is exactly the money staff must refund,
    /// and it has to show here. Never more than the price (the database's
    /// CK_Bookings_PaidAmount says the same): money beyond it - a second
    /// payment for a paid booking - stays on its Payment only, as a refund.
    /// </summary>
    /// <exception cref="DomainException">The booking would be paid more than its price.</exception>
    public void RecordPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        if (PaidAmount + amount > TotalAmount)
            throw new DomainException("booking_overpaid", "This payment is more than what is left to pay.");

        PaidAmount += amount;
    }

    /// <summary>
    /// Paid in full - called by the payment confirmation (Day 10). Allowed
    /// even a moment after the deadline as long as the expiry job hasn't run
    /// yet: the seats are still held, so nothing is lost.
    /// </summary>
    /// <exception cref="DomainException">Not waiting for payment, or not paid in full.</exception>
    public void Confirm(DateTime nowUtc, Guid? confirmedBy = null)
    {
        if (Status != BookingStatus.PendingPayment)
            throw new DomainException("booking_not_pending", "Only a booking waiting for payment can be confirmed.");
        EnsurePaidInFull();

        ChangeStatus(BookingStatus.Confirmed, nowUtc, confirmedBy, note: null);
        RaiseDomainEvent(new BookingConfirmed(Id));
    }

    /// <summary>
    /// The customer paid AFTER the hold ended and the booking expired (the
    /// gateway's page stays open longer than our 20 minutes). The caller must
    /// first take the seats again (TryReserveSeatsAsync). If they're gone,
    /// don't call this: the booking stays Expired and the money is refunded.
    /// </summary>
    /// <exception cref="DomainException">Not expired, or not paid in full.</exception>
    public void ConfirmAfterExpiry(DateTime nowUtc)
    {
        if (Status != BookingStatus.Expired)
            throw new DomainException("booking_not_expired", "Only an expired booking can be revived by a late payment.");
        EnsurePaidInFull();

        ChangeStatus(BookingStatus.Confirmed, nowUtc, changedBy: null, "Paid after the payment window ended.");
        RaiseDomainEvent(new BookingConfirmed(Id));
    }

    private void EnsurePaidInFull()
    {
        if (!IsPaidInFull)
            throw new DomainException("booking_not_paid", "The booking isn't paid in full yet.");
    }

    /// <summary>
    /// Cancelled by the customer or the agency, with a reason. Refunds are
    /// separate (Day 11-12). The handler releases SeatsHeld.
    /// </summary>
    public void Cancel(DateTime nowUtc, string reason, Guid? cancelledBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        reason = reason.Trim();
        if (reason.Length > 500) // CancelReason and the history Note are both 500 characters
            throw new ArgumentOutOfRangeException(nameof(reason), "At most 500 characters.");
        if (Status is not (BookingStatus.PendingPayment or BookingStatus.Confirmed))
            throw new DomainException("booking_not_cancellable", "This booking can no longer be cancelled.");

        CancelledAtUtc = nowUtc;
        CancelReason = reason;
        ChangeStatus(BookingStatus.Cancelled, nowUtc, cancelledBy, reason);
    }

    private void ChangeStatus(BookingStatus to, DateTime nowUtc, Guid? changedBy, string? note)
    {
        _history.Add(BookingStatusHistory.Record(Id, Status, to, nowUtc, changedBy, note));
        Status = to;
        HoldExpiresAtUtc = null; // a hold only means something while PendingPayment
    }

    // ---------- Rules shared by both factories ----------

    private static void EnsureForSale(TourPackage package)
    {
        if (package.Status != PackageStatus.Published)
            throw new DomainException("package_not_for_sale", "This package is not for sale.");
    }

    /// <summary>
    /// The counts come FROM the list of people, so "2 adults booked, 3 names
    /// sent" can't happen. Exactly one lead traveller, and an adult.
    /// Travellers.Create enforces the rest (at least one adult, an adult per
    /// infant, at most 20 people).
    /// </summary>
    private static Travellers CountTravellers(IReadOnlyList<TravellerDetails> travellers)
    {
        ArgumentNullException.ThrowIfNull(travellers);
        if (travellers.Any(t => !Enum.IsDefined(t.Type)))
            throw new ArgumentException("Unknown traveller type.", nameof(travellers));

        var leads = travellers.Where(t => t.IsLead).ToList();
        if (leads.Count != 1)
            throw new ArgumentException("Exactly one traveller must be the lead.", nameof(travellers));
        if (leads[0].Type != TravellerType.Adult)
            throw new ArgumentException("The lead traveller must be an adult.", nameof(travellers));

        return ValueObjects.Travellers.Create(
            travellers.Count(t => t.Type == TravellerType.Adult),
            travellers.Count(t => t.Type == TravellerType.Child),
            travellers.Count(t => t.Type == TravellerType.Infant));
    }

    public BookingAddOn AddAddOn(Guid addOnId, string nameSnapshot, decimal unitPrice, short quantity)
    {
        var addOn = BookingAddOn.Create(Id, addOnId, nameSnapshot, unitPrice, quantity);
        _addOns.Add(addOn);
        return addOn;
    }
}
