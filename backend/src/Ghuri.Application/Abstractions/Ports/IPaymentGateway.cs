using Ghuri.Domain.Enums;

namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// An online payment provider (blueprint: IPaymentGateway; SSLCommerz today,
/// Stripe in Phase 2). Application only knows this shape - which website to
/// call, which form fields it wants and how it answers stay inside
/// Infrastructure's SslCommerzGateway.
/// </summary>
public interface IPaymentGateway
{
    PaymentProvider Provider { get; }

    /// <summary>
    /// Asks the provider for a payment page for this payment. Never throws for
    /// a refusal or a network problem - those come back as a failed result
    /// with a reason, so the caller can record the attempt as Failed.
    /// </summary>
    Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// What to pay and who pays. TransactionId is our PaymentNo (PAY100001) - the
/// provider sends it back with every message, so we know which payment it is.
/// Reference is the booking number, echoed back too (for reconciliation).
/// </summary>
public sealed record PaymentSessionRequest(
    string TransactionId,
    string Reference,
    decimal Amount,
    string Currency,
    string ProductName,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone);

/// <summary>Either a payment page to send the customer to (and the provider's id for this session), or why there isn't one.</summary>
public sealed record PaymentSessionResult(bool Succeeded, string? PaymentPageUrl, string? SessionId, string? FailureReason)
{
    public static PaymentSessionResult Success(string paymentPageUrl, string sessionId) => new(true, paymentPageUrl, sessionId, null);

    public static PaymentSessionResult Failure(string reason) => new(false, null, null, reason);
}
