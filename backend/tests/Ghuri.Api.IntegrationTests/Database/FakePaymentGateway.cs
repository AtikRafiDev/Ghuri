using System.Collections.Concurrent;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Enums;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Stands in for SSLCommerz in the database tests - no internet, no sandbox.
/// It records every request, and answers with whatever the test sets in
/// Respond (by default: a made-up payment page for that transaction).
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.SslCommerz;

    public ConcurrentQueue<PaymentSessionRequest> Requests { get; } = new();

    public Func<PaymentSessionRequest, PaymentSessionResult> Respond { get; set; } = Accept;

    public static PaymentSessionResult Accept(PaymentSessionRequest request) =>
        PaymentSessionResult.Success($"https://sandbox.example/pay/{request.TransactionId}", $"session-{request.TransactionId}");

    /// <summary>Back to "accept everything" with no recorded requests - call at the start of a test.</summary>
    public void Reset()
    {
        Requests.Clear();
        Respond = Accept;
    }

    public Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);
        return Task.FromResult(Respond(request));
    }
}
