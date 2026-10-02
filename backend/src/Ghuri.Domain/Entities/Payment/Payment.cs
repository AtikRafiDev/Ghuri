using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Domain.Entities.Payment;

/// <summary>
/// Money received for a booking, through a gateway or manually (blueprint:
/// payment.Payments [A]). Its own aggregate root - it has its own
/// repository (IPaymentRepository).
/// </summary>
/// <remarks>
/// <para>
/// State machine: Initiated → Pending (the gateway gave us a payment page)
/// → Succeeded (Day 10, after the gateway's validation API confirms it).
/// Initiated or Pending → Failed / Cancelled.
/// </para>
/// <para>
/// "A late fail never downgrades a success": gateways send messages out of
/// order and more than once, so a fail or cancel arriving for a payment that
/// already succeeded is ignored - the money was taken. The reverse is
/// allowed (Day 10): a real, validated success after a "fail" still counts.
/// </para>
/// </remarks>
public sealed class Payment : AggregateRoot, IAuditable
{
    /// <summary>Human-readable code like PAY100001, generated from a SQL SEQUENCE (see AppDbContext). Sent to the gateway as tran_id.</summary>
    public string PaymentNo { get; private set; } = string.Empty;

    public Guid BookingId { get; private set; }
    public PaymentProvider Provider { get; private set; }

    /// <summary>bKash, Nagad, VISA... as reported by the gateway - not known until the customer actually pays.</summary>
    public string? Method { get; private set; }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BDT";
    public PaymentStatus Status { get; private set; }
    public string? ProviderSessionId { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public decimal? GatewayFee { get; private set; }
    public DateTime InitiatedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    private Payment()
    {
    }

    /// <summary>
    /// A new online payment for a booking. The amount and currency come FROM
    /// the booking - the browser never says how much to pay. Refused unless
    /// the booking is still waiting for payment inside its 20-minute hold.
    /// </summary>
    /// <exception cref="DomainException">The booking isn't waiting for payment, or its hold is over.</exception>
    public static Payment StartFor(BookingEntity booking, string paymentNo, PaymentProvider provider, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(booking);
        if (!booking.IsAwaitingPayment(nowUtc))
            throw new DomainException("booking_not_payable", "This booking can't be paid any more.");

        return Initiate(paymentNo, booking.Id, provider, booking.TotalAmount - booking.PaidAmount, booking.Currency, nowUtc);
    }

    public static Payment Initiate(string paymentNo, Guid bookingId, PaymentProvider provider, decimal amount, string currency, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentNo);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        return new Payment
        {
            PaymentNo = paymentNo,
            BookingId = bookingId,
            Provider = provider,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Initiated,
            InitiatedAtUtc = nowUtc
        };
    }

    /// <summary>The gateway created a payment page for us; <paramref name="sessionId"/> is its id for this attempt (SSLCommerz: sessionkey).</summary>
    public void MarkSessionCreated(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        if (sessionId.Length > 100) // ProviderSessionId column
            throw new ArgumentOutOfRangeException(nameof(sessionId), "At most 100 characters.");
        if (Status != PaymentStatus.Initiated)
            throw new DomainException("payment_not_initiated", "A payment page can only be created for a new payment.");

        ProviderSessionId = sessionId;
        Status = PaymentStatus.Pending;
    }

    /// <summary>The gateway refused the session, or the payment failed. Ignored once the payment succeeded (see remarks).</summary>
    public void MarkFailed(string reason) => End(PaymentStatus.Failed, reason);

    /// <summary>The customer cancelled on the gateway's page. Ignored once the payment succeeded (see remarks).</summary>
    public void MarkCancelled(string reason) => End(PaymentStatus.Cancelled, reason);

    private void End(PaymentStatus to, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        // Only a payment still in progress can end this way. Succeeded is
        // never downgraded, and repeating a fail/cancel changes nothing -
        // gateways resend messages, and a resend must not be an error.
        if (Status is not (PaymentStatus.Initiated or PaymentStatus.Pending))
            return;

        Status = to;
        reason = reason.Trim();
        FailureReason = reason.Length <= 300 ? reason : reason[..300]; // FailureReason column; gateway text can be long
    }
}
