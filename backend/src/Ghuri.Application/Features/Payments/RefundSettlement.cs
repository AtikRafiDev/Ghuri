using Ghuri.Application.Abstractions.Data;
using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Payments;

/// <summary>
/// A refund just completed: its payment becomes Refunded, or
/// PartiallyRefunded if only part of it went back. Shared by "Mark refunded"
/// (sent by hand) and SSLCommerz's "refunded" answer, so both count the same way.
/// </summary>
internal sealed class RefundSettlement(IPaymentRepository payments, IReadDbContext db)
{
    /// <summary>Call after the refund itself is marked completed, in the same command. Returns the payment, for the log.</summary>
    public async Task<Payment> ApplyAsync(Refund refund, CancellationToken cancellationToken)
    {
        // Everything already given back from this payment (committed earlier) + this one.
        var refundedBefore = await db.Refunds
            .Where(r => r.PaymentId == refund.PaymentId && r.Status == RefundStatus.Completed)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0;
        var payment = await payments.GetByIdAsync(refund.PaymentId, cancellationToken)
                      ?? throw new InvalidOperationException($"Refund {refund.RefundNo} has no payment.");
        payment.MarkRefunded(refundedBefore + refund.Amount);
        return payment;
    }
}
