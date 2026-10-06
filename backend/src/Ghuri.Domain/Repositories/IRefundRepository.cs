using Ghuri.Domain.Entities.Payment;

namespace Ghuri.Domain.Repositories;

/// <summary>The write side for refunds - money going back to a customer.</summary>
public interface IRefundRepository
{
    /// <summary>The next refund number - RF1001, RF1002... - from a database SEQUENCE.</summary>
    Task<string> NextRefundNoAsync(CancellationToken cancellationToken);

    void Add(Refund refund);
}
