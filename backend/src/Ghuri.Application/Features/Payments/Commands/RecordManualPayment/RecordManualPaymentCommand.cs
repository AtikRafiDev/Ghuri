using FluentValidation;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Booking;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.Extensions.Logging;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Application.Features.Payments.Commands.RecordManualPayment;

/// <summary>How money reached the agency outside SSLCommerz.</summary>
public enum ManualPaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    BKash = 3,
    Nagad = 4,
    Rocket = 5,
    Card = 6,
    Other = 7
}

/// <summary>
/// Staff record money paid straight to the agency - cash at the office, a
/// bank transfer, bKash to the agency's number (17-day plan, Day 12:
/// RecordManualPayment; also the fallback while SSLCommerz isn't live). The
/// booking is then confirmed exactly like an online payment: same domain
/// rules, same BookingConfirmed event, same voucher email.
/// </summary>
/// <param name="BookingNo">e.g. TB100001.</param>
/// <param name="Amount">Must be exactly what's due - partial payments are Phase 2.</param>
/// <param name="Method">Cash, bank transfer, bKash...</param>
/// <param name="Reference">The bank / bKash transaction id or receipt number. Optional for cash; the same reference can't be recorded twice.</param>
public sealed record RecordManualPaymentCommand(string BookingNo, decimal Amount, ManualPaymentMethod Method, string? Reference)
    : ICommand<RecordManualPaymentResponse>;

public sealed record RecordManualPaymentResponse(string PaymentNo, string BookingNo, BookingStatus BookingStatus);

internal sealed class RecordManualPaymentValidator : AbstractValidator<RecordManualPaymentCommand>
{
    public RecordManualPaymentValidator()
    {
        RuleFor(x => x.BookingNo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Reference).MaximumLength(100); // Payment.ProviderTransactionId
        RuleFor(x => x.Reference).NotEmpty()
            .When(x => x.Method is not (ManualPaymentMethod.Cash or ManualPaymentMethod.Other))
            .WithMessage("Enter the transaction id - it proves the money arrived.");
    }
}

/// <remarks>
/// <para>
/// The booking row is LOCKED first (like the gateway confirmation): an
/// online payment confirming the same booking at this moment waits, then
/// finds it paid - never two confirmations.
/// </para>
/// <para>
/// An EXPIRED booking can still be paid at the counter: its seats are taken
/// again first - if they're gone, nothing is recorded and staff are told.
/// </para>
/// </remarks>
internal sealed class RecordManualPaymentHandler(
    IBookingRepository bookings,
    IPaymentRepository payments,
    IDepartureRepository departures,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<RecordManualPaymentHandler> logger) : ICommandHandler<RecordManualPaymentCommand, RecordManualPaymentResponse>
{
    public async ValueTask<Result<RecordManualPaymentResponse>> Handle(RecordManualPaymentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } staffId)
            return IdentityErrors.NotAuthenticated;

        var booking = await bookings.GetByBookingNoForUpdateAsync(command.BookingNo, cancellationToken);
        if (booking is null)
            return BookingErrors.BookingNotFound;
        if (booking.Status is not (BookingStatus.PendingPayment or BookingStatus.Expired))
            return PaymentErrors.BookingNotPayable;

        var due = booking.TotalAmount - booking.PaidAmount;
        if (command.Amount != due)
            return PaymentErrors.ManualAmountMustBe(due, booking.Currency);

        var reference = string.IsNullOrWhiteSpace(command.Reference) ? null : command.Reference.Trim();
        if (reference is not null && await payments.PaymentNoWithReferenceAsync(reference, cancellationToken) is { } usedBy)
            return PaymentErrors.ReferenceAlreadyUsed(usedBy);

        // An expired booking gave its seats back - take them again, or stop here.
        if (booking.Status == BookingStatus.Expired && booking.SeatsHeld > 0
            && !await departures.TryReserveSeatsAsync(booking.DepartureId!.Value, booking.SeatsHeld, cancellationToken))
        {
            return PaymentErrors.SeatsNoLongerAvailable;
        }

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var payment = PaymentEntity.Initiate(
            await payments.NextPaymentNoAsync(cancellationToken), booking.Id, PaymentProvider.Manual, command.Amount, booking.Currency, nowUtc);
        payment.MarkSucceeded(MethodName(command.Method), reference, gatewayFee: null, nowUtc);
        payments.Add(payment);

        booking.RecordPayment(command.Amount);
        if (booking.Status == BookingStatus.Expired)
            booking.ConfirmAfterExpiry(nowUtc, staffId);
        else
            booking.Confirm(nowUtc, staffId);

        logger.LogInformation(
            "Manual payment {PaymentNo} ({Method}, {Amount}) confirmed booking {BookingNo}.",
            payment.PaymentNo, command.Method, command.Amount, booking.BookingNo);
        return new RecordManualPaymentResponse(payment.PaymentNo, booking.BookingNo, booking.Status);
    }

    /// <summary>Stored in Payment.Method, next to SSLCommerz's own names ("BKASH-BKash").</summary>
    private static string MethodName(ManualPaymentMethod method) => method switch
    {
        ManualPaymentMethod.Cash => "Cash",
        ManualPaymentMethod.BankTransfer => "Bank transfer",
        ManualPaymentMethod.BKash => "bKash (manual)",
        ManualPaymentMethod.Nagad => "Nagad (manual)",
        ManualPaymentMethod.Rocket => "Rocket (manual)",
        ManualPaymentMethod.Card => "Card (manual)",
        _ => "Other"
    };
}
