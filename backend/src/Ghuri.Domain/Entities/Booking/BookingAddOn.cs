using Ghuri.Domain.Common;

namespace Ghuri.Domain.Entities.Booking;

/// <summary>An add-on chosen for a booking, with a price snapshot. Owned by Booking - see BookingTraveller's remarks.</summary>
public sealed class BookingAddOn : BaseEntity
{
    public Guid BookingId { get; private set; }
    public Guid AddOnId { get; private set; }

    /// <summary>Copied at booking time, so a later rename of the add-on never changes this receipt.</summary>
    public string NameSnapshot { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }
    public short Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    private BookingAddOn()
    {
    }

    internal static BookingAddOn Create(Guid bookingId, Guid addOnId, string nameSnapshot, decimal unitPrice, short quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameSnapshot);
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        return new BookingAddOn
        {
            BookingId = bookingId,
            AddOnId = addOnId,
            NameSnapshot = nameSnapshot,
            UnitPrice = unitPrice,
            Quantity = quantity,
            LineTotal = unitPrice * quantity
        };
    }
}
