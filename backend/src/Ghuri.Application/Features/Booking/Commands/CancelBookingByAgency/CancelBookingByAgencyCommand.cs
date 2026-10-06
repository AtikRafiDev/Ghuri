using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Features.Booking.Commands.CancelBookingByAgency;

/// <summary>
/// Staff cancel a booking for the agency (17-day plan, Day 12:
/// CancelBookingByAgency) - e.g. the hotel of a flexible stay couldn't be
/// confirmed, or a departure is called off. The customer did nothing wrong,
/// so EVERYTHING paid comes back: a 100% refund, whatever the policy says.
/// </summary>
/// <param name="BookingNo">e.g. TB100001.</param>
/// <param name="Reason">Required - the customer and the audit trail see it.</param>
public sealed record CancelBookingByAgencyCommand(string BookingNo, string Reason) : ICommand<CancelBookingByAgencyResponse>;

/// <summary>RefundNo is null when nothing had been paid.</summary>
public sealed record CancelBookingByAgencyResponse(string BookingNo, decimal RefundAmount, string? RefundNo);

internal sealed class CancelBookingByAgencyValidator : AbstractValidator<CancelBookingByAgencyCommand>
{
    public CancelBookingByAgencyValidator()
    {
        RuleFor(x => x.BookingNo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500); // Booking.CancelReason
    }
}

/// <remarks>Locks the booking first, like the customer's cancel and the payment confirmation - one at a time.</remarks>
internal sealed class CancelBookingByAgencyHandler(
    IBookingRepository bookings,
    IDepartureRepository departures,
    IRefundRepository refunds,
    IReadDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<CancelBookingByAgencyHandler> logger) : ICommandHandler<CancelBookingByAgencyCommand, CancelBookingByAgencyResponse>
{
    public async ValueTask<Result<CancelBookingByAgencyResponse>> Handle(CancelBookingByAgencyCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } staffId)
            return IdentityErrors.NotAuthenticated;

        var booking = await bookings.GetByBookingNoForUpdateAsync(command.BookingNo, cancellationToken);
        if (booking is null)
            return BookingErrors.BookingNotFound;
        if (booking.Status is not (BookingStatus.PendingPayment or BookingStatus.Confirmed))
            return BookingErrors.NotCancellable("This booking can no longer be cancelled.");

        var reason = command.Reason.Trim();
        booking.Cancel(clock.GetUtcNow().UtcDateTime, reason, staffId);

        if (booking.SeatsHeld > 0
            && !await departures.ReleaseSeatsAsync(booking.DepartureId!.Value, booking.SeatsHeld, cancellationToken))
        {
            logger.LogWarning(
                "Booking {BookingNo} cancelled by staff, but its {Seats} seat(s) could not be released on departure {DepartureId}.",
                booking.BookingNo, booking.SeatsHeld, booking.DepartureId);
        }

        string? refundNo = null;
        if (booking.PaidAmount > 0)
        {
            var paymentId = await db.Payments
                .Where(p => p.BookingId == booking.Id && p.Status == PaymentStatus.Succeeded)
                .OrderByDescending(p => p.PaidAtUtc)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Booking {booking.BookingNo} is paid but has no succeeded payment.");

            refundNo = await refunds.NextRefundNoAsync(cancellationToken);
            refunds.Add(Refund.Request(refundNo, booking.Id, paymentId, booking.PaidAmount, 100, $"Cancelled by the agency: {reason}", staffId));
        }

        logger.LogInformation("Staff cancelled booking {BookingNo}; full refund {RefundAmount}.", booking.BookingNo, booking.PaidAmount);
        return new CancelBookingByAgencyResponse(booking.BookingNo, booking.PaidAmount, refundNo);
    }
}
