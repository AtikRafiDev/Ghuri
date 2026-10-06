using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Booking;

/// <summary>
/// One stop of a custom trip (blueprint: booking.CustomTripLegs): a
/// destination, how many nights there, and how to get on to the next stop.
/// Part of the CustomTrip aggregate - only CustomTrip creates them.
/// </summary>
/// <remarks>The dates are WORKED OUT by the trip (start date + the nights before this leg), never typed in.</remarks>
public sealed class CustomTripLeg : BaseEntity
{
    public Guid CustomTripId { get; private set; }

    /// <summary>1, 2, 3… - the order of the stops.</summary>
    public byte Sequence { get; private set; }

    public Guid DestinationId { get; private set; }
    public byte Nights { get; private set; }
    public TransferMode TransferToNext { get; private set; }
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }

    private CustomTripLeg()
    {
    }

    internal static CustomTripLeg Create(Guid customTripId, byte sequence, Guid destinationId, byte nights, TransferMode transferToNext, DateOnly checkIn) =>
        new()
        {
            CustomTripId = customTripId,
            Sequence = sequence,
            DestinationId = destinationId,
            Nights = nights,
            TransferToNext = transferToNext,
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(nights)
        };
}

/// <summary>One price line of a quote (blueprint: booking.CustomTripQuoteLines), e.g. Hotel · "Sea Pearl, 3 nights, 2 rooms" · ৳36,000.</summary>
public sealed class CustomTripQuoteLine : BaseEntity
{
    public Guid CustomTripId { get; private set; }
    public byte Sequence { get; private set; }
    public QuoteLineCategory Category { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }

    private CustomTripQuoteLine()
    {
    }

    internal static CustomTripQuoteLine Create(Guid customTripId, byte sequence, QuoteLineCategory category, string description, decimal amount) =>
        new()
        {
            CustomTripId = customTripId,
            Sequence = sequence,
            Category = category,
            Description = description,
            Amount = amount
        };
}

/// <summary>What the customer asks for, per stop - the input to CustomTrip.Submit.</summary>
public sealed record CustomTripLegRequest(Guid DestinationId, int Nights, TransferMode TransferToNext);

/// <summary>One price line staff type into a quote - the input to CustomTrip.Quote.</summary>
public sealed record QuoteLineInput(QuoteLineCategory Category, string Description, decimal Amount);

/// <summary>A customer sent a custom trip request → email staff and the customer (Day 13).</summary>
public sealed record CustomTripSubmitted(Guid CustomTripId) : IDomainEvent;

/// <summary>Staff sent (or re-sent) a quote → email the customer with the link (Day 13). QuoteVersion tells re-quotes apart.</summary>
public sealed record CustomTripQuoted(Guid CustomTripId, int QuoteVersion) : IDomainEvent;

/// <summary>Staff can't do the trip → tell the customer why.</summary>
public sealed record CustomTripRejected(Guid CustomTripId) : IDomainEvent;
