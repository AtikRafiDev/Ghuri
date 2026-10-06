using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Payments.Queries.GetPaymentResult;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Payments;

/// <summary>The customer's own payments. Login required. (Starting a payment is POST /api/v1/bookings/{bookingNo}/payments.)</summary>
[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// One of your own payments, e.g. /api/v1/payments/PAY100001:
    /// { paymentNo, status, amount, currency, bookingNo, bookingStatus }.
    /// The result page polls this until status is no longer 2 (pending). Someone else's is 404.
    /// </summary>
    [HttpGet("{paymentNo}")]
    public async Task<IActionResult> Get(string paymentNo, CancellationToken cancellationToken) =>
        (await sender.Send(new GetPaymentResultQuery(paymentNo), cancellationToken)).ToActionResult();
}
