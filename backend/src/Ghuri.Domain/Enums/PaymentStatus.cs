namespace Ghuri.Domain.Enums;

/// <summary>Maps to payment.Payments.Status (TINYINT).</summary>
public enum PaymentStatus : byte
{
    Initiated = 1,
    Pending = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5,
    Refunded = 6,
    PartiallyRefunded = 7
}
