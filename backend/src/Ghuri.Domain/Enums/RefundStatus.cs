namespace Ghuri.Domain.Enums;

/// <summary>Maps to payment.Refunds.Status (TINYINT).</summary>
public enum RefundStatus : byte
{
    Requested = 1,
    Approved = 2,
    Rejected = 3,
    Processing = 4,
    Completed = 5,
    Failed = 6
}
