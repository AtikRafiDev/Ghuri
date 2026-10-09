using FluentValidation;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Repositories;
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
/// money is never recorded as sent twice. A refund SSLCommerz is sending
/// is refused: it completes by itself (CheckGatewayRefund).
/// </remarks>
internal sealed class CompleteRefundHandler(
    IRefundRepository refunds,
    RefundSettlement settlement,
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
        if (refund.IsWithGateway)
            return PaymentErrors.RefundInProgress;

        refund.MarkCompleted(command.Reference, staffId, clock.GetUtcNow().UtcDateTime);
        var payment = await settlement.ApplyAsync(refund, cancellationToken);

        logger.LogInformation("Refund {RefundNo} ({Amount}) marked as sent; payment {PaymentNo} is now {Status}.",
            refund.RefundNo, refund.Amount, payment.PaymentNo, payment.Status);
        return Result.Success();
    }
}
