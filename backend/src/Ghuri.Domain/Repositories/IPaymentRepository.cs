using Ghuri.Domain.Entities.Payment;

namespace Ghuri.Domain.Repositories;

/// <summary>The write side for payments and the gateway messages about them.</summary>
public interface IPaymentRepository
{
    /// <summary>The next payment number - PAY100001, PAY100002... - from a database SEQUENCE. Sent to the gateway as tran_id.</summary>
    Task<string> NextPaymentNoAsync(CancellationToken cancellationToken);

    Task<Payment?> GetByPaymentNoAsync(string paymentNo, CancellationToken cancellationToken);

    void Add(Payment payment);

    /// <summary>Has this exact gateway message been saved before? (Provider + its id are unique.)</summary>
    Task<bool> EventExistsAsync(Enums.PaymentProvider provider, string providerEventId, CancellationToken cancellationToken);

    void AddEvent(PaymentEvent paymentEvent);
}
