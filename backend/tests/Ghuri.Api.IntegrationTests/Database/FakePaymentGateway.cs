using System.Collections.Concurrent;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Enums;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Stands in for SSLCommerz in the database tests - no internet, no sandbox.
/// It records every request, and answers with whatever the test sets in
/// Respond (by default: a made-up payment page for that transaction) and
/// Validate (by default: "not a valid payment").
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.SslCommerz;

    public ConcurrentQueue<PaymentSessionRequest> Requests { get; } = new();

    /// <summary>Every val_id the app asked about, in order.</summary>
    public ConcurrentQueue<string> Validations { get; } = new();

    public Func<PaymentSessionRequest, PaymentSessionResult> Respond { get; set; } = Accept;

    public Func<string, PaymentValidationResult> Validate { get; set; } = Refuse;

    public static PaymentSessionResult Accept(PaymentSessionRequest request) =>
        PaymentSessionResult.Success($"https://sandbox.example/pay/{request.TransactionId}", $"session-{request.TransactionId}");

    public static PaymentValidationResult Refuse(string validationId) =>
        PaymentValidationResult.Invalid("SSLCommerz says: INVALID_TRANSACTION.");

    /// <summary>From now on every val_id is "a real payment of this amount for this transaction" - as SSLCommerz would answer.</summary>
    public void ValidatesAs(string transactionId, decimal amount, string currency = "BDT", bool highRisk = false) =>
        Validate = validationId => PaymentValidationResult.Valid(
            // The bank's id is unique in the database, like SSLCommerz's real ones.
            transactionId, amount, currency, "BKASH-BKash", $"BANK-{transactionId}-{validationId}", gatewayFee: amount * 0.015m, highRisk);

    /// <summary>Back to the defaults with nothing recorded - call at the start of a test.</summary>
    public void Reset()
    {
        Requests.Clear();
        Validations.Clear();
        Respond = Accept;
        Validate = Refuse;
    }

    public Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);
        return Task.FromResult(Respond(request));
    }

    public Task<PaymentValidationResult> ValidatePaymentAsync(string validationId, CancellationToken cancellationToken)
    {
        Validations.Enqueue(validationId);
        return Task.FromResult(Validate(validationId));
    }
}
