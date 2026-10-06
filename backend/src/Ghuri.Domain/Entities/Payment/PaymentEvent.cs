using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Payment;

/// <summary>Raw gateway callback, stored before it's processed. A "log" table (BIGINT IDENTITY), same shape as iam.RefreshToken.</summary>
public sealed class PaymentEvent
{
    public long Id { get; private set; }

    /// <summary>Null until the event is matched to a known payment - a PaymentNo mismatch means it stays unmatched.</summary>
    public Guid? PaymentId { get; private set; }

    public PaymentProvider Provider { get; private set; }
    public PaymentEventType EventType { get; private set; }

    /// <summary>The gateway's own id for this event - together with Provider, this is what makes duplicate IPNs a no-op.</summary>
    public string ProviderEventId { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? ProcessingResult { get; private set; }

    private PaymentEvent()
    {
    }

    public static PaymentEvent Receive(
        PaymentProvider provider, PaymentEventType eventType, string providerEventId, string payloadJson, DateTime nowUtc, Guid? paymentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        return new PaymentEvent
        {
            PaymentId = paymentId,
            Provider = provider,
            EventType = eventType,
            ProviderEventId = providerEventId,
            PayloadJson = payloadJson,
            ReceivedAtUtc = nowUtc
        };
    }

    /// <summary>What we did with this message, e.g. "confirmed" or "rejected: amount 100.00 BDT, expected 34000.00 BDT". Cut to 300 characters.</summary>
    public void MarkProcessed(string result, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(result);
        result = result.Trim();

        ProcessingResult = result.Length <= 300 ? result : result[..300]; // ProcessingResult column
        ProcessedAtUtc = nowUtc;
    }
}
