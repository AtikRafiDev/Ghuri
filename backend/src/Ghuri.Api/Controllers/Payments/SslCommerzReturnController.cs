using Ghuri.Application.Features.Payments.Commands.RecordGatewayReturn;
using Ghuri.Domain.Enums;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Payments;

/// <summary>
/// Where SSLCommerz sends the customer's BROWSER back after its payment page
/// (the success_url / fail_url / cancel_url we gave it). The browser POSTs
/// SSLCommerz's form fields here; we record them and send the browser on to
/// the website's result page.
/// </summary>
/// <remarks>
/// No login: the request comes from SSLCommerz's page, and the customer's
/// login cookie isn't sent on a cross-site form POST. Safe because nothing
/// here is trusted - Day 10 confirms a payment only after asking SSLCommerz
/// itself (validation API), never because of what was posted here.
/// </remarks>
[ApiController]
[Route("api/v1/payments/sslcommerz")]
[AllowAnonymous]
public sealed class SslCommerzReturnController(ISender sender) : ControllerBase
{
    [HttpPost("success")]
    public Task<IActionResult> Success(CancellationToken cancellationToken) =>
        ReturnAsync(PaymentEventType.SuccessReturn, "success", cancellationToken);

    [HttpPost("fail")]
    public Task<IActionResult> Fail(CancellationToken cancellationToken) =>
        ReturnAsync(PaymentEventType.FailReturn, "fail", cancellationToken);

    [HttpPost("cancel")]
    public Task<IActionResult> Cancel(CancellationToken cancellationToken) =>
        ReturnAsync(PaymentEventType.CancelReturn, "cancel", cancellationToken);

    private async Task<IActionResult> ReturnAsync(PaymentEventType eventType, string outcome, CancellationToken cancellationToken)
    {
        var form = Request.HasFormContentType ? await Request.ReadFormAsync(cancellationToken) : null;
        var fields = form?.ToDictionary(field => field.Key, field => field.Value.ToString()) ?? [];

        await sender.Send(new RecordGatewayReturnCommand(eventType, fields), cancellationToken);

        // 303 See Other = "now GET this page" - the browser must not re-send
        // the form there. A relative address: the browser stays on our site
        // (locally http://localhost:5173, where React shows /payment/result).
        var paymentNo = Uri.EscapeDataString(fields.GetValueOrDefault("tran_id") ?? string.Empty);
        Response.Headers.Location = $"/payment/result?payment={paymentNo}&outcome={outcome}";
        return StatusCode(StatusCodes.Status303SeeOther);
    }
}
