using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Payment;

/// <summary>
/// Money received for a booking, through a gateway or manually (blueprint:
/// payment.Payments [A]). Its own aggregate root - it has its own
/// repository (IPaymentRepository).
/// </summary>
/// <remarks>
/// The state machine (Initiate -> Pending -> Succeeded | Failed |
/// Cancelled, with "a late fail never downgrades a success") is Day 9/10
/// work, built alongside the real SSLCommerz integration it protects.
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
}
