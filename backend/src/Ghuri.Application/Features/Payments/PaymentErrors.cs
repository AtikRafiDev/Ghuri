using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Payments;

/// <summary>Every expected failure when paying for a booking. Codes never change once shipped.</summary>
public static class PaymentErrors
{
    public static readonly Error BookingAlreadyPaid =
        Error.Conflict("booking_already_paid", "This booking is already paid.");

    /// <summary>The 20 minutes are over (or the booking was cancelled) - the seats may already be someone else's.</summary>
    public static readonly Error BookingHoldEnded =
        Error.Conflict("booking_hold_ended", "The time to pay for this booking has run out. Please book again.");

    /// <summary>SSLCommerz requires an email (it sends the receipt there).</summary>
    public static readonly Error ContactEmailRequired =
        Error.Failure("contact_email_required", "An email address is needed to pay online.");

    /// <summary>
    /// The gateway refused or couldn't be reached. CommitChanges: the payment
    /// is still saved - as Failed, with the gateway's reason - so staff can see
    /// the attempt. The customer gets a plain message; the technical reason
    /// stays in Payment.FailureReason.
    /// </summary>
    public static readonly Error PaymentStartFailed =
        Error.Failure("payment_start_failed", "We couldn't open the payment page right now. Please try again in a moment.")
            with { CommitChanges = true };

    /// <summary>
    /// The gateway's validation API didn't answer, so we can't tell yet
    /// whether a payment is real. NOT committed: the message isn't saved as
    /// handled, so the gateway's retry (or the other message) gets a fresh try.
    /// </summary>
    public static readonly Error GatewayUnreachable =
        Error.Failure("payment_gateway_unreachable", "We couldn't check the payment with the payment service. It will be checked again shortly.");

    public static readonly Error PaymentNotFound =
        Error.NotFound("payment_not_found", "This payment doesn't exist.");
}
