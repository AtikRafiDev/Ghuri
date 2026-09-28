using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Booking;

/// <summary>Every status transition a booking goes through - a "log" table (BIGINT IDENTITY), same shape as iam.RefreshToken.</summary>
public sealed class BookingStatusHistory
{
    public long Id { get; private set; }
    public Guid BookingId { get; private set; }
    public BookingStatus? FromStatus { get; private set; }
    public BookingStatus ToStatus { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    /// <summary>Null means a background job made the change, not a person.</summary>
    public Guid? ChangedBy { get; private set; }

    public string? Note { get; private set; }

    private BookingStatusHistory()
    {
    }

    internal static BookingStatusHistory Record(
        Guid bookingId, BookingStatus? fromStatus, BookingStatus toStatus, DateTime nowUtc, Guid? changedBy = null, string? note = null) =>
        new()
        {
            BookingId = bookingId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedAtUtc = nowUtc,
            ChangedBy = changedBy,
            Note = note
        };
}
