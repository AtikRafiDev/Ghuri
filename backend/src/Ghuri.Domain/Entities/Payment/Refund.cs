using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Entities.Payment;

/// <summary>Money returned to the customer (blueprint: payment.Refunds [A]). Its own aggregate root - has its own repository.</summary>
/// <remarks>
/// <para>
/// MVP state machine (17-day plan: "manual refunds"): Requested → Completed
/// (staff sent the money and typed its reference - MarkCompleted) or
/// Requested → Rejected (nothing is owed after all - Reject). Approved,
/// Processing and Failed belong to the Phase 2 approval workflow and
/// automatic gateway refunds.
/// </para>
/// <para>
/// Who asked: a customer (cancelling), a staff member (cancelling for the
/// agency), or nobody = the system (money that arrived too late or twice -
/// RequestedBy null).
/// </para>
/// </remarks>
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

    /// <summary>The customer or staff member who asked for it; null = the system (a late or double payment).</summary>
    public Guid? RequestedBy { get; private set; }

    /// <summary>The staff member who completed or rejected it.</summary>
    public Guid? ApprovedBy { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    /// <summary>The refund's own reference: a bKash / bank transaction id, or the gateway's refund id (Phase 2).</summary>
    public string? ProviderRefundId { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>Why it was rejected (or, Phase 2, why the gateway refund failed).</summary>
    public string? FailureReason { get; private set; }

    private Refund()
    {
    }

    public static Refund Request(
        string refundNo, Guid bookingId, Guid paymentId, decimal amount, decimal refundPercent,
        string reason, Guid? requestedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refundNo);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        if (refundPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(refundPercent), "RefundPercent must be between 0 and 100.");

        return new Refund
        {
            RefundNo = refundNo,
            BookingId = bookingId,
            PaymentId = paymentId,
            Amount = amount,
            RefundPercent = refundPercent,
            Reason = Cut(reason, 500),
            RequestedBy = requestedBy,
            Status = RefundStatus.Requested
        };
    }

    /// <summary>True while staff still have to act on it.</summary>
    public bool IsOpen => Status is RefundStatus.Requested or RefundStatus.Approved or RefundStatus.Processing;

    /// <summary>
    /// Staff sent the money back by hand (bKash, bank transfer, cash) - with
    /// the reference that proves it. Only once: a refund paid twice is money lost.
    /// </summary>
    /// <exception cref="DomainException">Already completed or rejected.</exception>
    public void MarkCompleted(string reference, Guid completedBy, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        EnsureOpen();

        Status = RefundStatus.Completed;
        ProviderRefundId = Cut(reference, 100);
        ApprovedBy = completedBy;
        ApprovedAtUtc = nowUtc;
        CompletedAtUtc = nowUtc;
    }

    /// <summary>Nothing is owed after all (e.g. a duplicate request). The reason is kept for the customer and the audit.</summary>
    /// <exception cref="DomainException">Already completed or rejected.</exception>
    public void Reject(string reason, Guid rejectedBy, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureOpen();

        Status = RefundStatus.Rejected;
        FailureReason = Cut(reason, 300);
        ApprovedBy = rejectedBy;
        ApprovedAtUtc = nowUtc;
    }

    private void EnsureOpen()
    {
        if (!IsOpen)
            throw new DomainException("refund_not_open", "This refund has already been completed or rejected.");
    }

    private static string Cut(string text, int maxLength)
    {
        text = text.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
