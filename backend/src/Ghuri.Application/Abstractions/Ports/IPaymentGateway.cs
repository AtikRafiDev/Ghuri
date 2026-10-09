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

    /// <summary>
    /// Asks the PROVIDER whether a payment really happened (SSLCommerz: the
    /// validation API, given the val_id from its message). This is the only answer
    /// we trust: the messages themselves can be posted by anyone. Never throws:
    /// a network problem comes back as Unreachable.
    /// </summary>
    Task<PaymentValidationResult> ValidatePaymentAsync(string validationId, CancellationToken cancellationToken);

    /// <summary>
    /// Asks the provider to send money of a succeeded payment back to the
    /// customer, the way they paid (SSLCommerz: the refund API). Never throws:
    /// no answer, or an answer we can't trust, comes back as Unconfirmed.
    /// </summary>
    Task<RefundStartResult> StartRefundAsync(RefundStartRequest request, CancellationToken cancellationToken);

    /// <summary>Asks the provider how a refund it accepted is going. Never throws: no answer is Unconfirmed.</summary>
    Task<RefundStatusResult> GetRefundStatusAsync(string providerRefundId, CancellationToken cancellationToken);
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

/// <summary>Valid = the provider confirms the money arrived · Invalid = it says no · Unreachable = no answer (ask again later).</summary>
public enum PaymentValidationOutcome
{
    Valid = 1,
    Invalid = 2,
    Unreachable = 3
}

/// <summary>
/// The provider's answer about one payment. When Valid: which transaction it
/// was (our PaymentNo), how much in which currency, and how it was paid. The
/// caller still checks that the transaction and amount are the ones it expects.
/// </summary>
public sealed record PaymentValidationResult(
    PaymentValidationOutcome Outcome,
    string? TransactionId = null,
    decimal? Amount = null,
    string? Currency = null,
    string? Method = null,
    string? ProviderTransactionId = null,
    decimal? GatewayFee = null,
    bool IsHighRisk = false,
    string? FailureReason = null)
{
    public static PaymentValidationResult Valid(
        string transactionId, decimal amount, string currency,
        string? method, string? providerTransactionId, decimal? gatewayFee, bool isHighRisk) =>
        new(PaymentValidationOutcome.Valid, transactionId, amount, currency, method, providerTransactionId, gatewayFee, isHighRisk);

    public static PaymentValidationResult Invalid(string reason) => new(PaymentValidationOutcome.Invalid, FailureReason: reason);

    public static PaymentValidationResult Unreachable(string reason) => new(PaymentValidationOutcome.Unreachable, FailureReason: reason);
}

/// <summary>
/// One refund to send. RefundId is our RefundNo (RF1001): the provider keeps
/// it with the refund, so asking twice for the same refund is recognised as
/// the same one. ProviderTransactionId is the provider's id of the PAYMENT
/// (SSLCommerz bank_tran_id). Reference is the booking number, for reconciliation.
/// </summary>
public sealed record RefundStartRequest(
    string RefundId,
    string ProviderTransactionId,
    decimal Amount,
    string Reason,
    string Reference);

/// <summary>
/// Started = the provider accepted it and is sending the money ·
/// Refused = it said no, nothing was sent · Unconfirmed = no answer, or one
/// we can't act on (nothing is recorded; ask again later).
/// </summary>
public enum RefundStartOutcome
{
    Started = 1,
    Refused = 2,
    Unconfirmed = 3
}

/// <summary>When Started: the provider's id for this refund (SSLCommerz refund_ref_id). Otherwise: why not, in words staff can read.</summary>
public sealed record RefundStartResult(RefundStartOutcome Outcome, string? ProviderRefundId = null, string? FailureReason = null)
{
    public static RefundStartResult Started(string providerRefundId) => new(RefundStartOutcome.Started, providerRefundId);

    public static RefundStartResult Refused(string reason) => new(RefundStartOutcome.Refused, FailureReason: reason);

    public static RefundStartResult Unconfirmed(string reason) => new(RefundStartOutcome.Unconfirmed, FailureReason: reason);
}

/// <summary>
/// Refunded = the money is back with the customer · Processing = still on its
/// way · Failed = the provider cancelled it, the money was NOT sent ·
/// Unconfirmed = no answer, or one we can't act on (ask again later).
/// </summary>
public enum RefundStatusOutcome
{
    Refunded = 1,
    Processing = 2,
    Failed = 3,
    Unconfirmed = 4
}

/// <summary>The provider's answer about one refund. FailureReason says why when Failed or Unconfirmed.</summary>
public sealed record RefundStatusResult(RefundStatusOutcome Outcome, string? FailureReason = null)
{
    public static readonly RefundStatusResult Refunded = new(RefundStatusOutcome.Refunded);

    public static readonly RefundStatusResult Processing = new(RefundStatusOutcome.Processing);

    public static RefundStatusResult Failed(string reason) => new(RefundStatusOutcome.Failed, reason);

    public static RefundStatusResult Unconfirmed(string reason) => new(RefundStatusOutcome.Unconfirmed, reason);
}
