using Ghuri.Domain.Entities.Payment;

namespace Ghuri.Domain.Repositories;

/// <summary>The write side for payments and the gateway messages about them.</summary>
public interface IPaymentRepository
{
    /// <summary>The next payment number - PAY100001, PAY100002... - from a database SEQUENCE. Sent to the gateway as tran_id.</summary>
    Task<string> NextPaymentNoAsync(CancellationToken cancellationToken);

    Task<Payment?> GetByPaymentNoAsync(string paymentNo, CancellationToken cancellationToken);

    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Is this reference (a bank / bKash transaction id) already on another payment? Stops one receipt being recorded twice.</summary>
    Task<string?> PaymentNoWithReferenceAsync(string reference, CancellationToken cancellationToken);

    /// <summary>
    /// Like GetByPaymentNoAsync, but LOCKS the row until the transaction ends.
    /// Two messages about the same payment (the IPN and the browser coming back
    /// at the same moment) are then handled one after the other: the second
    /// sees what the first did.
    /// </summary>
    Task<Payment?> GetByPaymentNoForUpdateAsync(string paymentNo, CancellationToken cancellationToken);

    void Add(Payment payment);

    /// <summary>Has this exact gateway message been saved before? (Provider + its id are unique.)</summary>
    Task<bool> EventExistsAsync(Enums.PaymentProvider provider, string providerEventId, CancellationToken cancellationToken);

    void AddEvent(PaymentEvent paymentEvent);
}
