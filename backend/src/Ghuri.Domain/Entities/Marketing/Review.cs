using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Marketing;

/// <summary>A customer's rating of a completed trip (blueprint: marketing.Reviews [A]). Its own aggregate root.</summary>
/// <remarks>The rule "only for Completed bookings of the same user" is enforced by the command handler that creates these, not here - this class only guards its own internal shape (a rating between 1 and 5, etc.).</remarks>
public sealed class Review : AggregateRoot, IAuditable
{
    public Guid PackageId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public byte Rating { get; private set; }
    public string? Title { get; private set; }
    public string? Comment { get; private set; }
    public ReviewStatus Status { get; private set; }
    public Guid? ModeratedBy { get; private set; }
    public DateTime? ModeratedAtUtc { get; private set; }

    private Review()
    {
    }

    public static Review Create(Guid packageId, Guid bookingId, Guid userId, byte rating, string? title = null, string? comment = null)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        return new Review
        {
            PackageId = packageId,
            BookingId = bookingId,
            UserId = userId,
            Rating = rating,
            Title = title,
            Comment = comment,
            Status = ReviewStatus.Pending
        };
    }
}
