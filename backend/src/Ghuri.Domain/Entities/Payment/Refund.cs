using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Payment;

/// <summary>Money returned to the customer (blueprint: payment.Refunds [A]). Its own aggregate root - has its own repository.</summary>
/// <remarks>The Request -> Approve/Reject -> Processing -> Completed|Failed state machine is Day 10/11 work.</remarks>
public sealed class Refund : AggregateRoot, IAuditable
{
    /// <summary>Human-readable code like RF1001, generated from a SQL SEQUENCE (see AppDbContext).</summary>
    public string RefundNo { get; private set; } = string.Empty;

    public Guid BookingId { get; private set; }
    public Guid PaymentId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal RefundPercent { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public RefundStatus Status { get; private set; }
    public Guid RequestedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? ProviderRefundId { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    private Refund()
    {
    }

    public static Refund Request(
        string refundNo, Guid bookingId, Guid paymentId, decimal amount, decimal refundPercent,
        string reason, Guid requestedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refundNo);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        return new Refund
        {
            RefundNo = refundNo,
            BookingId = bookingId,
            PaymentId = paymentId,
            Amount = amount,
            RefundPercent = refundPercent,
            Reason = reason,
            RequestedBy = requestedBy,
            Status = RefundStatus.Requested
        };
    }
}
