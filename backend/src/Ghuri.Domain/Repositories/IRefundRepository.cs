using Ghuri.Domain.Entities.Payment;

namespace Ghuri.Domain.Repositories;

/// <summary>The write side for refunds - money going back to a customer.</summary>
public interface IRefundRepository
{
    /// <summary>The next refund number - RF1001, RF1002... - from a database SEQUENCE.</summary>
    Task<string> NextRefundNoAsync(CancellationToken cancellationToken);

    /// <summary>
    /// A refund by its number (RF1001), tracked and LOCKED until the
    /// transaction ends: two staff clicking "Mark refunded" at once are
    /// handled one after the other - the second sees it's already done.
    /// </summary>
    Task<Refund?> GetByRefundNoForUpdateAsync(string refundNo, CancellationToken cancellationToken);

    void Add(Refund refund);
}
