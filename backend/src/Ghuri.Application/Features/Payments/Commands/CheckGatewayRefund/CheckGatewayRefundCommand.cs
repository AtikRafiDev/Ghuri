using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Features.Payments.Commands.CheckGatewayRefund;

/// <summary>
/// The refunds SSLCommerz is still sending, oldest first. Read by the refund
/// status job, which then checks them one by one.
/// </summary>
public sealed record GetRefundsWithGatewayQuery(int Max) : IQuery<IReadOnlyList<string>>, IQuietMessage;

internal sealed class GetRefundsWithGatewayHandler(IReadDbContext db) : IQueryHandler<GetRefundsWithGatewayQuery, IReadOnlyList<string>>
{
    public async ValueTask<Result<IReadOnlyList<string>>> Handle(GetRefundsWithGatewayQuery query, CancellationToken cancellationToken) =>
        await db.Refunds
            .Where(r => r.Status == RefundStatus.Processing)
            .OrderBy(r => r.ApprovedAtUtc)
            .Take(query.Max)
            .Select(r => r.RefundNo)
            .ToListAsync(cancellationToken);
}

/// <summary>
/// Asks SSLCommerz how a refund it's sending is going. Done → Completed, and
/// its payment Refunded. Cancelled → Failed: still owed, so staff try again or
/// send it by hand. Still on its way → nothing changes. Run by the refund
/// status job every few minutes, and by staff's "Check now".
/// </summary>
public sealed record CheckGatewayRefundCommand(string RefundNo) : ICommand<CheckGatewayRefundResponse>;

/// <summary>The refund's status after the check: Completed, Failed, or still Processing.</summary>
public sealed record CheckGatewayRefundResponse(string RefundNo, RefundStatus Status);

internal sealed class CheckGatewayRefundValidator : AbstractValidator<CheckGatewayRefundCommand>
{
    public CheckGatewayRefundValidator()
    {
        RuleFor(x => x.RefundNo).NotEmpty().MaximumLength(20);
    }
}

/// <remarks>
/// Locked like the other refund commands: the job and a staff member
/// checking at the same moment run one after the other, so "refunded" is
/// counted once. A refund that isn't with SSLCommerz any more (the other one
/// got there first) is left alone.
/// </remarks>
internal sealed class CheckGatewayRefundHandler(
    IRefundRepository refunds,
    IPaymentGateway gateway,
    RefundSettlement settlement,
    TimeProvider clock,
    ILogger<CheckGatewayRefundHandler> logger) : ICommandHandler<CheckGatewayRefundCommand, CheckGatewayRefundResponse>
{
    public async ValueTask<Result<CheckGatewayRefundResponse>> Handle(CheckGatewayRefundCommand command, CancellationToken cancellationToken)
    {
        var refund = await refunds.GetByRefundNoForUpdateAsync(command.RefundNo, cancellationToken);
        if (refund is null)
            return PaymentErrors.RefundNotFound;
        if (!refund.IsWithGateway)
            return new CheckGatewayRefundResponse(refund.RefundNo, refund.Status);

        var answer = await gateway.GetRefundStatusAsync(refund.ProviderRefundId!, cancellationToken);
        switch (answer.Outcome)
        {
            case RefundStatusOutcome.Refunded:
                refund.MarkGatewayRefunded(clock.GetUtcNow().UtcDateTime);
                var payment = await settlement.ApplyAsync(refund, cancellationToken);
                logger.LogInformation("Refund {RefundNo} ({Amount}) is back with the customer; payment {PaymentNo} is now {Status}.",
                    refund.RefundNo, refund.Amount, payment.PaymentNo, payment.Status);
                break;

            case RefundStatusOutcome.Failed:
                refund.MarkGatewayFailed(answer.FailureReason!);
                logger.LogWarning("Refund {RefundNo} failed at {Provider}: {Reason} It's back on the list to process.",
                    refund.RefundNo, gateway.Provider, answer.FailureReason);
                break;

            case RefundStatusOutcome.Unconfirmed:
                return PaymentErrors.RefundUnconfirmed(answer.FailureReason ?? "SSLCommerz could not be reached.");

            // Processing: still on its way - nothing to do.
        }

        return new CheckGatewayRefundResponse(refund.RefundNo, refund.Status);
    }
}
