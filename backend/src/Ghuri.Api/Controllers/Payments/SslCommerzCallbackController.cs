using Ghuri.Application.Features.Payments.Commands.HandleGatewayCallback;
using Ghuri.Domain.Enums;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Payments;

/// <summary>
/// Where SSLCommerz talks to us, in two ways:
/// <list type="bullet">
/// <item>The IPN: SSLCommerz's SERVER posts "payment done" to ipn_url. It
/// expects 200 and resends after anything else.</item>
/// <item>The returns: the customer's BROWSER comes back from SSLCommerz's page
/// (success_url / fail_url / cancel_url) with SSLCommerz's form fields; we
/// handle them and send the browser on to the website's result page.</item>
/// </list>
/// All of them go through HandleGatewayCallbackCommand, which trusts
/// nothing posted here: a payment is confirmed only after SSLCommerz's
/// validation API says so.
/// </summary>
/// <remarks>
/// No login: the requests come from SSLCommerz, and the customer's login
/// cookie isn't sent on a cross-site form POST.
/// </remarks>
[ApiController]
[Route("api/v1/payments/sslcommerz")]
[AllowAnonymous]
public sealed class SslCommerzCallbackController(ISender sender) : ControllerBase
{
    /// <summary>
    /// SSLCommerz's server-to-server notification. 200 = handled (confirmed,
    /// rejected or a repeat - all final). 503 = we couldn't check it with
    /// SSLCommerz right now: SSLCommerz sends it again later.
    /// </summary>
    [HttpPost("ipn")]
    public async Task<IActionResult> Ipn(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new HandleGatewayCallbackCommand(PaymentEventType.Ipn, await ReadFormAsync(cancellationToken)), cancellationToken);
        return result.IsSuccess ? Ok() : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

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
        var fields = await ReadFormAsync(cancellationToken);

        // The result is ignored on purpose: if SSLCommerz didn't answer, the
        // customer still goes to the result page, which keeps asking, and the
        // IPN confirms the payment anyway.
        await sender.Send(new HandleGatewayCallbackCommand(eventType, fields), cancellationToken);

        // 303 See Other = "now GET this page" - the browser must not re-send
        // the form there. A relative address: the browser stays on our site
        // (locally http://localhost:5173, where React shows /payment/result).
        var paymentNo = Uri.EscapeDataString(fields.GetValueOrDefault("tran_id") ?? string.Empty);
        Response.Headers.Location = $"/payment/result?payment={paymentNo}&outcome={outcome}";
        return StatusCode(StatusCodes.Status303SeeOther);
    }

    private async Task<Dictionary<string, string>> ReadFormAsync(CancellationToken cancellationToken)
    {
        var form = Request.HasFormContentType ? await Request.ReadFormAsync(cancellationToken) : null;
        return form?.ToDictionary(field => field.Key, field => field.Value.ToString()) ?? [];
    }
}
