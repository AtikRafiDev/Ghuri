namespace Ghuri.Domain.Enums;

/// <summary>Maps to payment.Payments.Provider (TINYINT).</summary>
public enum PaymentProvider : byte
{
    SslCommerz = 1,
    Stripe = 2,
    Manual = 3
}
