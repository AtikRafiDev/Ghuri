using System.Globalization;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Booking;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using PaymentEntity = Ghuri.Domain.Entities.Payment.Payment;

namespace Ghuri.Application.Features.Payments.Commands.InitiatePayment;

/// <summary>
/// "Pay now" for one of the customer's bookings (17-day plan, Day 9:
/// InitiatePayment). Creates a payment and asks the gateway for its payment
/// page. Every click is a new attempt with its own number; the payment
/// confirmation (Day 10) makes sure only one of them can confirm the booking.
/// </summary>
public sealed record InitiatePaymentCommand(string BookingNo) : ICommand<InitiatePaymentResponse>;

/// <summary>Where to send the customer's browser, and our number for this attempt.</summary>
public sealed record InitiatePaymentResponse(string PaymentNo, string PaymentPageUrl);

/// <remarks>
/// The gateway is called INSIDE the command, but before anything is saved:
/// TransactionBehavior saves only when the handler returns, so no row is
/// locked while we wait for SSLCommerz.
/// </remarks>
internal sealed class InitiatePaymentHandler(
    IBookingRepository bookings,
    IPaymentRepository payments,
    IPaymentGateway gateway,
    IReadDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<InitiatePaymentCommand, InitiatePaymentResponse>
{
    public async ValueTask<Result<InitiatePaymentResponse>> Handle(InitiatePaymentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        // Someone else's booking is "not found", like GetMyBooking.
        var booking = await bookings.GetByBookingNoAsync(command.BookingNo, cancellationToken);
        if (booking is null || booking.CustomerId != customerId)
            return BookingErrors.BookingNotFound;

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        if (booking.Status == BookingStatus.Confirmed)
            return PaymentErrors.BookingAlreadyPaid;
        if (!booking.IsAwaitingPayment(nowUtc))
            return PaymentErrors.BookingHoldEnded;
        if (string.IsNullOrWhiteSpace(booking.ContactEmail))
            return PaymentErrors.ContactEmailRequired;

        // The amount and currency come from the booking (Payment.StartFor).
        var payment = PaymentEntity.StartFor(booking, await payments.NextPaymentNoAsync(cancellationToken), gateway.Provider, nowUtc);
        payments.Add(payment);

        var session = await gateway.CreateSessionAsync(
            new PaymentSessionRequest(
                TransactionId: payment.PaymentNo,
                Reference: booking.BookingNo,
                Amount: payment.Amount,
                Currency: payment.Currency,
                ProductName: await ProductNameAsync(booking.PackageId, booking.StartDate, booking.BookingNo, cancellationToken),
                CustomerName: booking.ContactName,
                CustomerEmail: booking.ContactEmail,
                CustomerPhone: booking.ContactPhone.Value),
            cancellationToken);

        if (!session.Succeeded)
        {
            // Saved anyway (CommitChanges) - staff see the attempt and the gateway's reason.
            payment.MarkFailed(session.FailureReason ?? "The payment gateway gave no reason.");
            return PaymentErrors.PaymentStartFailed;
        }

        payment.MarkSessionCreated(session.SessionId!);
        return new InitiatePaymentResponse(payment.PaymentNo, session.PaymentPageUrl!);
    }

    /// <summary>
    /// What the customer sees on the gateway's page and receipt: "Beach Escape - 20 Dec 2026 - TB100001".
    /// InvariantCulture: English month names whatever the server's language settings.
    /// </summary>
    private async Task<string> ProductNameAsync(Guid? packageId, DateOnly startDate, string bookingNo, CancellationToken cancellationToken)
    {
        var title = packageId is { } id
            ? await db.TourPackages.Where(p => p.Id == id).Select(p => p.Title).FirstOrDefaultAsync(cancellationToken)
            : null;
        return $"{title ?? "Custom trip"} - {startDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture)} - {bookingNo}";
    }
}
