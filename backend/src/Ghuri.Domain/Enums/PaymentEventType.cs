namespace Ghuri.Domain.Enums;

/// <summary>Maps to payment.PaymentEvents.EventType (TINYINT).</summary>
public enum PaymentEventType : byte
{
    Ipn = 1,
    SuccessReturn = 2,
    FailReturn = 3,
    CancelReturn = 4,
    RefundCallback = 5
}
