using System.Globalization;
using System.Text.Json;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.Extensions.Logging;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Application.Features.Payments.Commands.HandleGatewayCallback;

/// <summary>
/// A message from SSLCommerz about one payment (17-day plan, Day 10:
/// HandleGatewayIpn). Two ways it arrives, handled the same way:
/// the IPN (SSLCommerz's server calls ours) and the customer's browser coming
/// back (success / fail / cancel). Whichever comes first confirms the
/// booking; the other then changes nothing.
/// </summary>
/// <remarks>
/// <para>
/// Steps: save the raw message once (PaymentEvent, unique per message) →
/// for an IPN or a success return carrying a val_id, ask SSLCommerz's
/// validation API whether the payment is real → check it's OUR transaction,
/// for the right amount and currency → payment Succeeded, booking Confirmed.
/// </para>
/// <para>
/// Nothing in the posted fields is trusted: anyone can POST them. Only the
/// validation API's answer can confirm a payment, and fail / cancel messages
/// are only recorded (a fake "fail" must not undo anything).
/// </para>
/// <para>
/// Late payment: SSLCommerz's page stays open longer than our 20-minute hold.
/// If the booking expired meanwhile, we try to take its seats again. If we
/// get them, the booking is revived (Confirmed). If not, the money is kept
/// on the booking (PaidAmount) and logged as a refund for staff (Day 12).
/// The same goes for money arriving for a cancelled booking. A second
/// payment for a booking that is already paid stays on its own Payment
/// (Succeeded) and is logged as a refund too.
/// </para>
/// </remarks>
public sealed record HandleGatewayCallbackCommand(PaymentEventType EventType, IReadOnlyDictionary<string, string> Fields) : ICommand;

/// <remarks>
/// Order matters for speed and safety:
/// 1. SSLCommerz is asked BEFORE any row is locked. The answer takes a
///    second or so, and nobody should wait on our locks meanwhile.
/// 2. Then the payment row is locked (UPDLOCK). Two messages about the same
///    payment now run one after the other, and the duplicate check after the
///    lock sees what the first one did.
/// 3. The booking row is locked too, so the expiry job can't expire it
///    mid-confirmation (it waits, then finds it Confirmed).
/// </remarks>
internal sealed class HandleGatewayCallbackHandler(
    IPaymentRepository payments,
    IBookingRepository bookings,
    IDepartureRepository departures,
    IPaymentGateway gateway,
    TimeProvider clock,
    ILogger<HandleGatewayCallbackHandler> logger) : ICommandHandler<HandleGatewayCallbackCommand>
{
    public async ValueTask<Result> Handle(HandleGatewayCallbackCommand command, CancellationToken cancellationToken)
    {
        var transactionId = Field(command, "tran_id");
        if (transactionId is null)
            return Result.Success(); // nothing to tie it to - not even worth keeping

        // "Ipn:PAY100001:<val_id>" - the same message twice (SSLCommerz
        // retrying, a refresh, the back button) is handled once: (Provider, id) is unique.
        var validationId = Field(command, "val_id");
        var eventId = Truncate($"{command.EventType}:{transactionId}:{validationId ?? "-"}", 100);
        if (await payments.EventExistsAsync(PaymentProvider.SslCommerz, eventId, cancellationToken))
            return Result.Success(); // the common repeat - skip it before asking SSLCommerz anything

        // 1. Ask SSLCommerz first - no locks held while we wait.
        PaymentValidationResult? validation = null;
        if (command.EventType is PaymentEventType.Ipn or PaymentEventType.SuccessReturn && validationId is not null)
        {
            validation = await gateway.ValidatePaymentAsync(validationId, cancellationToken);
            if (validation.Outcome == PaymentValidationOutcome.Unreachable)
                return PaymentErrors.GatewayUnreachable; // rolled back: the message stays unhandled, a retry will try again
        }

        // 2. One message per payment at a time from here on.
        var payment = await payments.GetByPaymentNoForUpdateAsync(transactionId, cancellationToken);
        if (await payments.EventExistsAsync(PaymentProvider.SslCommerz, eventId, cancellationToken))
            return Result.Success(); // the same message was handled while we waited for the lock

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var paymentEvent = PaymentEvent.Receive(
            PaymentProvider.SslCommerz, command.EventType, eventId,
            JsonSerializer.Serialize(command.Fields), nowUtc, payment?.Id);
        payments.AddEvent(paymentEvent);

        var outcome = validation is null
            ? "recorded" // fail / cancel / no val_id: kept for staff, changes nothing
            : await ApplyAsync(payment, validation, nowUtc, cancellationToken);
        paymentEvent.MarkProcessed(outcome, nowUtc);

        return Result.Success();
    }

    /// <summary>Acts on SSLCommerz's answer. Returns what happened, saved as the event's ProcessingResult.</summary>
    private async Task<string> ApplyAsync(PaymentEntity? payment, PaymentValidationResult validation, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (payment is null)
            return "rejected: unknown payment number";
        if (validation.Outcome != PaymentValidationOutcome.Valid)
            return $"rejected: {validation.FailureReason}";

        // The val_id must belong to THIS payment - otherwise someone could
        // reuse a real val_id from a cheaper payment to "pay" an expensive one.
        if (!string.Equals(validation.TransactionId, payment.PaymentNo, StringComparison.OrdinalIgnoreCase))
            return Rejected(payment, $"rejected: the validation is for transaction {validation.TransactionId}");

        if (validation.Amount != payment.Amount
            || !string.Equals(validation.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return Rejected(payment,
                $"rejected: amount {Money(validation.Amount)} {validation.Currency}, expected {Money(payment.Amount)} {payment.Currency}");
        }

        if (payment.Status == PaymentStatus.Succeeded)
            return "already confirmed"; // the IPN and the browser both brought it - the first one did the work
        if (payment.Status is PaymentStatus.Refunded or PaymentStatus.PartiallyRefunded)
            return Rejected(payment, "ignored: the payment was already refunded");

        payment.MarkSucceeded(validation.Method, validation.ProviderTransactionId, validation.GatewayFee, nowUtc);
        var risk = validation.IsHighRisk ? " (SSLCommerz: high risk - check before the trip)" : string.Empty;

        // 3. The booking - locked, so the expiry job waits for us.
        var booking = await bookings.GetByIdForUpdateAsync(payment.BookingId, cancellationToken)
                      ?? throw new InvalidOperationException($"Payment {payment.PaymentNo} has no booking.");

        // Another attempt already paid it (the customer paid on two payment
        // pages). This money stays on its own Payment and goes back.
        if (booking.PaidAmount + payment.Amount > booking.TotalAmount)
            return RefundDue(payment, booking.BookingNo, $"paid, but the booking was already paid ({booking.Status})");

        booking.RecordPayment(payment.Amount);

        if (booking.Status == BookingStatus.PendingPayment && booking.IsPaidInFull)
        {
            booking.Confirm(nowUtc);
            logger.LogInformation("Payment {PaymentNo} confirmed booking {BookingNo}.", payment.PaymentNo, booking.BookingNo);
            return "confirmed" + risk;
        }

        if (booking.Status == BookingStatus.Expired && booking.IsPaidInFull)
        {
            // Paid late. Take the seats again - if they're still free.
            if (booking.SeatsHeld == 0
                || await departures.TryReserveSeatsAsync(booking.DepartureId!.Value, booking.SeatsHeld, cancellationToken))
            {
                booking.ConfirmAfterExpiry(nowUtc);
                logger.LogInformation("Late payment {PaymentNo} revived expired booking {BookingNo}.", payment.PaymentNo, booking.BookingNo);
                return "confirmed after expiry" + risk;
            }

            return RefundDue(payment, booking.BookingNo, "paid after the booking expired, and its seats are gone");
        }

        // Cancelled while the customer was paying: the money must go back.
        return RefundDue(payment, booking.BookingNo, $"paid, but the booking is {booking.Status}");
    }

    private string Rejected(PaymentEntity payment, string outcome)
    {
        // Worth a warning: a wrong amount or someone else's val_id is either a bug or someone trying it on.
        logger.LogWarning("SSLCommerz message for payment {PaymentNo} {Outcome}.", payment.PaymentNo, outcome);
        return outcome;
    }

    private string RefundDue(PaymentEntity payment, string bookingNo, string why)
    {
        logger.LogWarning(
            "REFUND DUE: payment {PaymentNo} ({Amount} {Currency}) for booking {BookingNo} was {Why}.",
            payment.PaymentNo, Money(payment.Amount), payment.Currency, bookingNo, why);
        return $"refund due: {why}";
    }

    private static string? Field(HandleGatewayCallbackCommand command, string name) =>
        command.Fields.GetValueOrDefault(name) is { } value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static string Money(decimal? amount) => amount?.ToString("0.00", CultureInfo.InvariantCulture) ?? "?";

    private static string Truncate(string text, int maxLength) => text.Length <= maxLength ? text : text[..maxLength];
}
