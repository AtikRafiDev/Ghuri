using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Features.Payments.Commands.CompleteRefund;

/// <summary>
/// Staff sent a refund by hand and record its reference (17-day plan,
/// Day 12: MarkRefunded - "manual refund reference"). The refund becomes
/// Completed; its payment becomes Refunded, or PartiallyRefunded if only
/// part of it went back.
/// </summary>
/// <param name="RefundNo">e.g. RF1001.</param>
/// <param name="Reference">The bKash / bank transaction id of the money sent back - proof for the customer and the books.</param>
public sealed record CompleteRefundCommand(string RefundNo, string Reference) : ICommand;

internal sealed class CompleteRefundValidator : AbstractValidator<CompleteRefundCommand>
{
    public CompleteRefundValidator()
    {
        RuleFor(x => x.RefundNo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(100); // Refund.ProviderRefundId
    }
}

/// <remarks>
/// The refund row is LOCKED first: two staff clicking at the same moment
/// run one after the other, and the second gets "already completed" - the
/// money is never recorded as sent twice.
/// </remarks>
internal sealed class CompleteRefundHandler(
    IRefundRepository refunds,
    IPaymentRepository payments,
    IReadDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<CompleteRefundHandler> logger) : ICommandHandler<CompleteRefundCommand>
{
    public async ValueTask<Result> Handle(CompleteRefundCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } staffId)
            return IdentityErrors.NotAuthenticated;

        var refund = await refunds.GetByRefundNoForUpdateAsync(command.RefundNo, cancellationToken);
        if (refund is null)
            return PaymentErrors.RefundNotFound;
        if (!refund.IsOpen)
            return PaymentErrors.RefundNotOpen;

        refund.MarkCompleted(command.Reference, staffId, clock.GetUtcNow().UtcDateTime);

        // Everything already given back from this payment (committed earlier) + this one.
        var refundedBefore = await db.Refunds
            .Where(r => r.PaymentId == refund.PaymentId && r.Status == RefundStatus.Completed)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0;
        var payment = await payments.GetByIdAsync(refund.PaymentId, cancellationToken)
                      ?? throw new InvalidOperationException($"Refund {refund.RefundNo} has no payment.");
        payment.MarkRefunded(refundedBefore + refund.Amount);

        logger.LogInformation("Refund {RefundNo} ({Amount}) marked as sent; payment {PaymentNo} is now {Status}.",
            refund.RefundNo, refund.Amount, payment.PaymentNo, payment.Status);
        return Result.Success();
    }
}
