using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Entities.Payment;

/// <summary>Money returned to the customer (blueprint: payment.Refunds [A]). Its own aggregate root - has its own repository.</summary>
/// <remarks>
/// <para>
/// By hand (17-day plan: "manual refunds"): Requested → Completed (staff
/// sent the money and typed its reference - MarkCompleted) or Requested →
/// Rejected (nothing is owed after all - Reject).
/// </para>
/// <para>
/// Through SSLCommerz (for money that came in through SSLCommerz): Requested
/// → Processing (SSLCommerz accepted the refund - MarkSentToGateway) →
/// Completed (SSLCommerz says the money is back - MarkGatewayRefunded), or →
/// Failed (SSLCommerz refused or cancelled it - MarkGatewayFailed). Failed
/// is still owed, so it stays open: staff try again or send it by hand.
/// While Processing, staff can't complete or reject it - SSLCommerz is
/// already sending the money, and sending it by hand too would pay twice.
/// Approved belongs to the Phase 2 approval workflow.
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

    /// <summary>The staff member who completed, rejected or sent it to SSLCommerz.</summary>
    public Guid? ApprovedBy { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    /// <summary>The refund's own reference: a bKash / bank transaction id, or SSLCommerz's refund_ref_id.</summary>
    public string? ProviderRefundId { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>Why it was rejected, or why SSLCommerz refused or cancelled it.</summary>
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

    /// <summary>True until the money is back or it's rejected: still owed to the customer.</summary>
    public bool IsOpen => Status is RefundStatus.Requested or RefundStatus.Approved or RefundStatus.Processing or RefundStatus.Failed;

    /// <summary>SSLCommerz is sending the money: only its answer can complete or fail it now.</summary>
    public bool IsWithGateway => Status == RefundStatus.Processing;

    /// <summary>
    /// Staff sent the money back by hand (bKash, bank transfer, cash) - with
    /// the reference that proves it. Only once: a refund paid twice is money lost.
    /// </summary>
    /// <exception cref="DomainException">Already completed or rejected, or SSLCommerz is sending it.</exception>
    public void MarkCompleted(string reference, Guid completedBy, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        EnsureStaffCanAct();

        Status = RefundStatus.Completed;
        ProviderRefundId = Cut(reference, 100);
        ApprovedBy = completedBy;
        ApprovedAtUtc = nowUtc;
        CompletedAtUtc = nowUtc;
    }

    /// <summary>Nothing is owed after all (e.g. a duplicate request). The reason is kept for the customer and the audit.</summary>
    /// <exception cref="DomainException">Already completed or rejected, or SSLCommerz is sending it.</exception>
    public void Reject(string reason, Guid rejectedBy, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureStaffCanAct();

        Status = RefundStatus.Rejected;
        FailureReason = Cut(reason, 300);
        ApprovedBy = rejectedBy;
        ApprovedAtUtc = nowUtc;
    }

    /// <summary>
    /// SSLCommerz accepted the refund and is sending the money back the way
    /// the customer paid. <paramref name="providerRefundId"/> is its
    /// refund_ref_id - what we ask about later. A refund that failed before
    /// can be sent again.
    /// </summary>
    /// <exception cref="DomainException">Already completed or rejected, or SSLCommerz is sending it.</exception>
    public void MarkSentToGateway(string providerRefundId, Guid sentBy, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerRefundId);
        EnsureStaffCanAct();

        Status = RefundStatus.Processing;
        ProviderRefundId = Cut(providerRefundId, 100);
        ApprovedBy = sentBy;
        ApprovedAtUtc = nowUtc;
        FailureReason = null; // a new try - the last one's reason no longer applies
    }

    /// <summary>SSLCommerz says the money is back with the customer.</summary>
    /// <exception cref="DomainException">It wasn't with SSLCommerz.</exception>
    public void MarkGatewayRefunded(DateTime nowUtc)
    {
        if (!IsWithGateway)
            throw new DomainException("refund_not_with_gateway", "This refund isn't being sent by SSLCommerz.");

        Status = RefundStatus.Completed;
        CompletedAtUtc = nowUtc;
    }

    /// <summary>
    /// SSLCommerz refused to start the refund, or cancelled it later. The
    /// money is still owed, so it stays open (see remarks), with the reason.
    /// </summary>
    /// <exception cref="DomainException">Already completed or rejected.</exception>
    public void MarkGatewayFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (!IsOpen)
            throw new DomainException("refund_not_open", "This refund has already been completed or rejected.");

        Status = RefundStatus.Failed;
        FailureReason = Cut(reason, 300);
    }

    private void EnsureStaffCanAct()
    {
        if (!IsOpen)
            throw new DomainException("refund_not_open", "This refund has already been completed or rejected.");
        if (IsWithGateway)
            throw new DomainException("refund_in_progress", "SSLCommerz is already sending this refund.");
    }

    private static string Cut(string text, int maxLength)
    {
        text = text.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
