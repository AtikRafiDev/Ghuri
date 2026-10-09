using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Payments.Commands.CheckGatewayRefund;
using Ghuri.Application.Features.Payments.Commands.CompleteRefund;
using Ghuri.Application.Features.Payments.Commands.RejectRefund;
using Ghuri.Application.Features.Payments.Commands.StartGatewayRefund;
using Ghuri.Application.Features.Payments.Queries.SearchPayments;
using Ghuri.Application.Features.Payments.Queries.SearchRefunds;
using Ghuri.Domain.Enums;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Admin;

/// <summary>
/// Payments and refunds for staff (17-day plan, Day 12). Every staff member
/// can look; sending a refund (by hand or through SSLCommerz) or rejecting it needs ManageMoney.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = Policies.AdminArea)]
[Authorize(Policy = Policies.ViewBookings)]
public sealed class AdminPaymentsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Every payment attempt, newest first.
    /// ?search=PAY…|TB…|transaction id&amp;status=1..7&amp;provider=1 SSLCommerz · 3 manual&amp;from=2026-10-01&amp;to=2026-10-31&amp;page=1&amp;pageSize=20
    /// </summary>
    [HttpGet("payments")]
    public async Task<IActionResult> Payments(
        [FromQuery] string? search, [FromQuery] PaymentStatus? status, [FromQuery] PaymentProvider? provider,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        (await sender.Send(new SearchPaymentsQuery(search, status, provider, from, to, page, pageSize), cancellationToken))
        .ToActionResult();

    /// <summary>?open=true = still to process (oldest first); otherwise all refunds (newest first). &amp;search=RF…|TB…|name</summary>
    [HttpGet("refunds")]
    public async Task<IActionResult> Refunds(
        [FromQuery] string? search, CancellationToken cancellationToken,
        [FromQuery] bool open = true, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        (await sender.Send(new SearchRefundsQuery(open, search, page, pageSize), cancellationToken)).ToActionResult();

    /// <summary>The money was sent back - body { "reference": "bKash / bank transaction id" }. 409 refund_not_open if already done.</summary>
    [HttpPost("refunds/{refundNo}/complete")]
    [Authorize(Policy = Policies.ManageMoney)]
    public async Task<IActionResult> Complete(string refundNo, CompleteRefundRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new CompleteRefundCommand(refundNo, request.Reference), cancellationToken)).ToActionResult();

    /// <summary>
    /// Send it back through SSLCommerz, the way the customer paid. 204 = SSLCommerz
    /// accepted it (now Processing). 409 refund_not_online = paid by hand ·
    /// 400 refund_refused = SSLCommerz said no (saved as Failed) · 400 refund_unconfirmed = no answer, try again.
    /// </summary>
    [HttpPost("refunds/{refundNo}/sslcommerz")]
    [Authorize(Policy = Policies.ManageMoney)]
    public async Task<IActionResult> SendThroughGateway(string refundNo, CancellationToken cancellationToken) =>
        (await sender.Send(new StartGatewayRefundCommand(refundNo), cancellationToken)).ToActionResult();

    /// <summary>"Check now": asks SSLCommerz how the refund is going → { refundNo, status }. The job does the same every 15 minutes.</summary>
    [HttpPost("refunds/{refundNo}/sslcommerz/check")]
    [Authorize(Policy = Policies.ManageMoney)]
    public async Task<IActionResult> CheckGateway(string refundNo, CancellationToken cancellationToken) =>
        (await sender.Send(new CheckGatewayRefundCommand(refundNo), cancellationToken)).ToActionResult();

    /// <summary>Not owed after all - body { "reason": "..." }.</summary>
    [HttpPost("refunds/{refundNo}/reject")]
    [Authorize(Policy = Policies.ManageMoney)]
    public async Task<IActionResult> Reject(string refundNo, RejectRefundRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new RejectRefundCommand(refundNo, request.Reason), cancellationToken)).ToActionResult();
}

public sealed record CompleteRefundRequest(string Reference);

public sealed record RejectRefundRequest(string Reason);
