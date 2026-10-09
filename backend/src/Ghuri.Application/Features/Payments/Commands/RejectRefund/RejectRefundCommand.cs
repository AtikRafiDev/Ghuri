using FluentValidation;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Features.Payments.Commands.RejectRefund;

/// <summary>
/// Staff decide a refund isn't owed after all (Day 12) - e.g. the customer
/// was already refunded another way. The reason is kept on the refund for
/// the customer and the audit trail. Nothing else changes.
/// </summary>
public sealed record RejectRefundCommand(string RefundNo, string Reason) : ICommand;

internal sealed class RejectRefundValidator : AbstractValidator<RejectRefundCommand>
{
    public RejectRefundValidator()
    {
        RuleFor(x => x.RefundNo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300); // Refund.FailureReason
    }
}

internal sealed class RejectRefundHandler(
    IRefundRepository refunds,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<RejectRefundHandler> logger) : ICommandHandler<RejectRefundCommand>
{
    public async ValueTask<Result> Handle(RejectRefundCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } staffId)
            return IdentityErrors.NotAuthenticated;

        // Locked like CompleteRefund: a refund is either sent or rejected, never both.
        var refund = await refunds.GetByRefundNoForUpdateAsync(command.RefundNo, cancellationToken);
        if (refund is null)
            return PaymentErrors.RefundNotFound;
        if (!refund.IsOpen)
            return PaymentErrors.RefundNotOpen;
        if (refund.IsWithGateway)
            return PaymentErrors.RefundInProgress; // SSLCommerz is sending it - it can't be called back from here

        refund.Reject(command.Reason, staffId, clock.GetUtcNow().UtcDateTime);

        logger.LogInformation("Refund {RefundNo} rejected.", refund.RefundNo);
        return Result.Success();
    }
}
