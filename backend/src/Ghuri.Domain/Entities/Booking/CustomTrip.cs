using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Booking;

/// <summary>
/// A multi-destination trip the customer designs and staff price by hand
/// (17-day plan, option B, Day 13; blueprint: booking.CustomTrips). The
/// aggregate root for its legs (the stops, in order) and its quote lines.
/// </summary>
/// <remarks>
/// <para>
/// Life: Submitted → Quoted → Accepted → Paid (Accept/Paid arrive on Day 15).
/// Staff may also Reject it; a quote not accepted in time Expires; the
/// customer may Cancel before paying. A quote can be replaced while it's
/// Quoted or Expired (decided 2026-10-06) - QuoteVersion counts them.
/// </para>
/// <para>
/// All dates come from the start date + each leg's nights - worked out here,
/// so the customer, staff and the voucher can never disagree.
/// </para>
/// </remarks>
public sealed class CustomTrip : AggregateRoot, IAuditable
{
    public const int MaxLegs = 10;
    public const int MaxNightsPerLeg = 30;
    public const int MaxTotalNights = 60;

    /// <summary>Staff need time to quote (target 24 h) and the customer to accept and pay.</summary>
    public const int MinLeadDays = 3;

    public const int MaxQuoteLines = 30;
    public const int DefaultQuoteValidDays = 3; // the plan's risk table: "quotes valid 3 days"
    public const int MaxQuoteValidDays = 14;

    /// <summary>Human-readable code like CT1001, generated from a SQL SEQUENCE (see AppDbContext).</summary>
    public string TripNo { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }
    public CustomTripStatus Status { get; private set; }

    public DateOnly StartDate { get; private set; }

    /// <summary>StartDate + all the nights - the day the trip ends.</summary>
    public DateOnly EndDate { get; private set; }

    public byte TotalNights { get; private set; }

    public byte Adults { get; private set; }
    public byte Children { get; private set; }
    public byte Infants { get; private set; }

    public HotelLevel HotelLevel { get; private set; }

    /// <summary>Optional: what the customer hopes to spend per person - a hint for staff, not a limit.</summary>
    public decimal? BudgetPerPerson { get; private set; }

    public string? Notes { get; private set; }

    public string ContactName { get; private set; } = string.Empty;
    public PhoneNumber ContactPhone { get; private set; } = null!;
    public string? ContactEmail { get; private set; }

    public string Currency { get; private set; } = "BDT";
    public DateTime SubmittedAtUtc { get; private set; }

    // ----- The current quote (replaced as a whole on a re-quote) -----

    /// <summary>0 = never quoted; 1, 2… = how many quotes were sent.</summary>
    public int QuoteVersion { get; private set; }

    public string? QuoteItinerary { get; private set; }

    /// <summary>The sum of the quote lines - what the customer pays when they accept.</summary>
    public decimal? QuoteTotal { get; private set; }

    public DateTime? QuotedAtUtc { get; private set; }
    public Guid? QuotedBy { get; private set; }
    public DateTime? QuoteExpiresAtUtc { get; private set; }

    // ----- How it ended -----

    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? ExpiredAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public string? RejectReason { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelReason { get; private set; }

    private readonly List<CustomTripLeg> _legs = [];
    public IReadOnlyList<CustomTripLeg> Legs => _legs.AsReadOnly();

    private readonly List<CustomTripQuoteLine> _quoteLines = [];
    public IReadOnlyList<CustomTripQuoteLine> QuoteLines => _quoteLines.AsReadOnly();

    private CustomTrip()
    {
    }

    /// <summary>
    /// A new request: the stops in order, with their dates worked out from
    /// <paramref name="startDate"/>. Raises CustomTripSubmitted.
    /// </summary>
    /// <exception cref="DomainException">Too soon, too many stops or nights, or a stop without nights.</exception>
    public static CustomTrip Submit(
        string tripNo, Guid customerId, DateOnly startDate, Travellers travellers, HotelLevel hotelLevel,
        decimal? budgetPerPerson, string? notes, BookingContact contact, IReadOnlyList<CustomTripLegRequest> legs,
        DateOnly today, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tripNo);
        ArgumentNullException.ThrowIfNull(travellers);
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentException.ThrowIfNullOrWhiteSpace(contact.Name);
        ArgumentNullException.ThrowIfNull(contact.Phone);
        ArgumentNullException.ThrowIfNull(legs);
        if (!Enum.IsDefined(hotelLevel))
            throw new ArgumentOutOfRangeException(nameof(hotelLevel));

        if (startDate < today.AddDays(MinLeadDays))
            throw new DomainException("trip_start_too_soon", $"A custom trip can start {MinLeadDays} days from today at the earliest.");
        if (legs.Count is 0 or > MaxLegs)
            throw new DomainException("trip_legs_count", $"Add 1 to {MaxLegs} destinations.");
        if (legs.Any(l => l.Nights is < 1 or > MaxNightsPerLeg))
            throw new DomainException("trip_leg_nights", $"Each destination needs 1 to {MaxNightsPerLeg} nights.");
        if (legs.Sum(l => l.Nights) > MaxTotalNights)
            throw new DomainException("trip_too_long", $"A custom trip can be at most {MaxTotalNights} nights.");
        if (legs.Any(l => l.DestinationId == Guid.Empty || !Enum.IsDefined(l.TransferToNext)))
            throw new ArgumentException("Every leg needs a destination and a transfer.", nameof(legs));
        if (budgetPerPerson is <= 0)
            throw new ArgumentOutOfRangeException(nameof(budgetPerPerson), "The budget must be greater than zero.");

        var trip = new CustomTrip
        {
            TripNo = tripNo,
            CustomerId = customerId,
            Status = CustomTripStatus.Submitted,
            StartDate = startDate,
            Adults = (byte)travellers.Adults,
            Children = (byte)travellers.Children,
            Infants = (byte)travellers.Infants,
            HotelLevel = hotelLevel,
            BudgetPerPerson = budgetPerPerson,
            Notes = Clean(notes, 2000),
            ContactName = contact.Name.Trim(),
            ContactPhone = contact.Phone,
            ContactEmail = Clean(contact.Email, 256),
            SubmittedAtUtc = nowUtc
        };

        // Each stop starts the day the previous one ends; the last one has no transfer onwards.
        var checkIn = startDate;
        for (var i = 0; i < legs.Count; i++)
        {
            var leg = legs[i];
            var transfer = i == legs.Count - 1 ? TransferMode.None : leg.TransferToNext;
            var created = CustomTripLeg.Create(trip.Id, (byte)(i + 1), leg.DestinationId, (byte)leg.Nights, transfer, checkIn);
            trip._legs.Add(created);
            checkIn = created.CheckOutDate;
        }

        trip.TotalNights = (byte)legs.Sum(l => l.Nights);
        trip.EndDate = checkIn;
        trip.RaiseDomainEvent(new CustomTripSubmitted(trip.Id));
        return trip;
    }

    /// <summary>
    /// Staff price the trip: the itinerary, the price lines, how long it's
    /// valid. A new quote REPLACES the previous one completely (lines,
    /// total, deadline). Raises CustomTripQuoted.
    /// </summary>
    /// <exception cref="DomainException">Not open for quoting, no lines, or an invalid validity.</exception>
    public void Quote(string itinerary, IReadOnlyList<QuoteLineInput> lines, int validDays, Guid quotedBy, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itinerary);
        ArgumentNullException.ThrowIfNull(lines);
        if (Status is not (CustomTripStatus.Submitted or CustomTripStatus.Quoted or CustomTripStatus.Expired))
            throw new DomainException("trip_not_quotable", "Only a request waiting for a price can be quoted.");
        if (lines.Count is 0 or > MaxQuoteLines)
            throw new DomainException("quote_lines_count", $"A quote needs 1 to {MaxQuoteLines} price lines.");
        if (lines.Any(l => l.Amount <= 0 || string.IsNullOrWhiteSpace(l.Description) || !Enum.IsDefined(l.Category)))
            throw new DomainException("quote_line_invalid", "Every price line needs a category, a description and an amount above zero.");
        if (validDays is < 1 or > MaxQuoteValidDays)
            throw new DomainException("quote_validity", $"A quote can be valid for 1 to {MaxQuoteValidDays} days.");

        _quoteLines.Clear();
        for (var i = 0; i < lines.Count; i++)
            _quoteLines.Add(CustomTripQuoteLine.Create(Id, (byte)(i + 1), lines[i].Category, Clean(lines[i].Description, 200)!, lines[i].Amount));

        Status = CustomTripStatus.Quoted;
        QuoteVersion++;
        QuoteItinerary = Clean(itinerary, 4000);
        QuoteTotal = lines.Sum(l => l.Amount);
        QuotedAtUtc = nowUtc;
        QuotedBy = quotedBy;
        QuoteExpiresAtUtc = nowUtc.AddDays(validDays);
        ExpiredAtUtc = null;
        RaiseDomainEvent(new CustomTripQuoted(Id, QuoteVersion));
    }

    /// <summary>Staff can't do it - the reason goes to the customer. Raises CustomTripRejected.</summary>
    /// <exception cref="DomainException">Already accepted, paid, rejected or cancelled.</exception>
    public void Reject(string reason, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status is not (CustomTripStatus.Submitted or CustomTripStatus.Quoted or CustomTripStatus.Expired))
            throw new DomainException("trip_not_rejectable", "This request can no longer be rejected.");

        Status = CustomTripStatus.Rejected;
        RejectedAtUtc = nowUtc;
        RejectReason = Clean(reason, 500);
        RaiseDomainEvent(new CustomTripRejected(Id));
    }

    /// <summary>The customer withdraws it - only before accepting (after that it's a booking, cancelled like one).</summary>
    /// <exception cref="DomainException">Already accepted, paid, rejected or cancelled.</exception>
    public void Cancel(string? reason, DateTime nowUtc)
    {
        if (Status is not (CustomTripStatus.Submitted or CustomTripStatus.Quoted or CustomTripStatus.Expired))
            throw new DomainException("trip_not_cancellable", "This request can no longer be cancelled.");

        Status = CustomTripStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        CancelReason = Clean(reason, 500);
    }

    // ----- Accept and pay (Day 15) -----

    /// <summary>
    /// The customer takes the offer: Quoted → Accepted. A booking is made
    /// from it right after (Booking.CreateForCustomTrip) and paid like any other.
    /// </summary>
    /// <exception cref="DomainException">No live quote, or it ran out (even if the hourly job hasn't marked it yet).</exception>
    public void Accept(DateTime nowUtc)
    {
        if (IsQuoteOverdue(nowUtc) || Status == CustomTripStatus.Expired)
            throw new DomainException("quote_expired", "This quote has run out. Please ask us for a new price.");
        if (Status != CustomTripStatus.Quoted)
            throw new DomainException("trip_not_acceptable", "There is no quote to accept on this trip.");

        Status = CustomTripStatus.Accepted;
        AcceptedAtUtc = nowUtc;
    }

    /// <summary>
    /// The booking made on accepting was never paid (its 20 minutes ran out):
    /// back to Quoted while the offer still stands, else Expired - the customer
    /// can accept again, or ask for a new price. Does nothing unless Accepted.
    /// </summary>
    public void ReleaseAcceptance(DateTime nowUtc)
    {
        if (Status != CustomTripStatus.Accepted)
            return;

        AcceptedAtUtc = null;
        if (QuoteExpiresAtUtc <= nowUtc)
        {
            Status = CustomTripStatus.Expired;
            ExpiredAtUtc = nowUtc;
        }
        else
        {
            Status = CustomTripStatus.Quoted;
        }
    }

    /// <summary>
    /// Its booking was paid (the payment confirmation, through the outbox) - the
    /// trip is on. Also from Quoted/Expired: money that arrives after the hold
    /// was released still counts (Day 10's late payment). A repeat does nothing.
    /// </summary>
    /// <returns>False if the trip was already cancelled or rejected - the booking is paid anyway; staff must sort it out.</returns>
    public bool MarkPaid(DateTime nowUtc)
    {
        if (Status == CustomTripStatus.Paid)
            return true;
        if (Status is CustomTripStatus.Cancelled or CustomTripStatus.Rejected)
            return false;

        Status = CustomTripStatus.Paid;
        AcceptedAtUtc ??= nowUtc;
        PaidAtUtc = nowUtc;
        return true;
    }

    /// <summary>Its booking was cancelled (by the customer or the agency): the trip is off too.</summary>
    public void CancelWithBooking(string reason, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status is CustomTripStatus.Cancelled or CustomTripStatus.Rejected)
            return;

        Status = CustomTripStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        CancelReason = Clean(reason, 500);
    }

    /// <summary>True once a sent quote is past its deadline - even before the expiry job has marked it.</summary>
    public bool IsQuoteOverdue(DateTime nowUtc) => Status == CustomTripStatus.Quoted && QuoteExpiresAtUtc <= nowUtc;

    /// <summary>The quote ran out (the hourly job). Does nothing if it isn't overdue - safe to run twice.</summary>
    public void ExpireQuote(DateTime nowUtc)
    {
        if (!IsQuoteOverdue(nowUtc))
            return;

        Status = CustomTripStatus.Expired;
        ExpiredAtUtc = nowUtc;
    }

    private static string? Clean(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        text = text.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
