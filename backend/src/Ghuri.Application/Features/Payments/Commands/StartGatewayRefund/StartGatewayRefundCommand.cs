using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Features.Payments.Commands.StartGatewayRefund;

/// <summary>
/// Staff send a refund back through SSLCommerz - to the bKash, Nagad or card
/// the customer paid with - instead of by hand. SSLCommerz accepts it (the
/// refund becomes Processing) and sends the money; CheckGatewayRefund later
/// sees it's done and completes it.
/// </summary>
/// <param name="RefundNo">e.g. RF1001. Its payment must have come in through SSLCommerz.</param>
public sealed record StartGatewayRefundCommand(string RefundNo) : ICommand;

internal sealed class StartGatewayRefundValidator : AbstractValidator<StartGatewayRefundCommand>
{
    public StartGatewayRefundValidator()
    {
        RuleFor(x => x.RefundNo).NotEmpty().MaximumLength(20);
    }
}

/// <remarks>
/// The refund row stays LOCKED while SSLCommerz answers (a few seconds): a
/// double click, or a second staff member, waits and then sees it's already
/// being sent. Here the lock is the point - two refunds for one is money
/// lost. (InitiatePayment does the opposite: it locks nothing while it waits.)
/// </remarks>
internal sealed class StartGatewayRefundHandler(
    IRefundRepository refunds,
    IPaymentGateway gateway,
    IReadDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<StartGatewayRefundHandler> logger) : ICommandHandler<StartGatewayRefundCommand>
{
    public async ValueTask<Result> Handle(StartGatewayRefundCommand command, CancellationToken cancellationToken)
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

        var paid = await (
                from p in db.Payments
                where p.Id == refund.PaymentId
                join b in db.Bookings on p.BookingId equals b.Id
                select new { p.Provider, p.ProviderTransactionId, b.BookingNo })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Refund {refund.RefundNo} has no payment.");
        if (paid.Provider != gateway.Provider)
            return PaymentErrors.RefundNotOnline;
        if (string.IsNullOrWhiteSpace(paid.ProviderTransactionId))
            return PaymentErrors.RefundNoBankTransaction;

        var answer = await gateway.StartRefundAsync(
            new RefundStartRequest(refund.RefundNo, paid.ProviderTransactionId, refund.Amount, refund.Reason, paid.BookingNo),
            cancellationToken);

        switch (answer.Outcome)
        {
            case RefundStartOutcome.Started:
                refund.MarkSentToGateway(answer.ProviderRefundId!, staffId, clock.GetUtcNow().UtcDateTime);
                logger.LogInformation("Refund {RefundNo} ({Amount}) sent to {Provider}; its id there is {ProviderRefundId}.",
                    refund.RefundNo, refund.Amount, gateway.Provider, answer.ProviderRefundId);
                return Result.Success();

            case RefundStartOutcome.Refused:
                // Saved (CommitChanges): Failed, with the reason - still owed, still on the "to process" list.
                refund.MarkGatewayFailed(answer.FailureReason!);
                return PaymentErrors.RefundRefused(answer.FailureReason!);

            default:
                return PaymentErrors.RefundUnconfirmed(answer.FailureReason ?? "SSLCommerz didn't confirm the refund. Please try again in a moment.");
        }
    }
}
