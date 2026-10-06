using FluentValidation;

namespace Ghuri.Application.Features.Booking.Commands.CancelMyBooking;

internal sealed class CancelMyBookingValidator : AbstractValidator<CancelMyBookingCommand>
{
    public CancelMyBookingValidator()
    {
        RuleFor(x => x.BookingNo).NotEmpty().MaximumLength(20);

        // Booking.CancelReason and the history note are 500 characters.
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
