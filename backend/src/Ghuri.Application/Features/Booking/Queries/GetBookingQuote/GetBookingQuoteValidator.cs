using FluentValidation;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Booking.Queries.GetBookingQuote;

/// <summary>The same traveller rules as the Travellers value object, as field errors the form can show.</summary>
internal sealed class GetBookingQuoteValidator : AbstractValidator<GetBookingQuoteQuery>
{
    public GetBookingQuoteValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(220);

        RuleFor(x => x.Adults).InclusiveBetween(1, Travellers.MaxPerBooking).WithMessage("At least one adult must travel.");
        RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Infants).GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(x => x.Adults).WithMessage("Each infant needs an adult to travel with.");
        RuleFor(x => x.Adults + x.Children + x.Infants)
            .LessThanOrEqualTo(Travellers.MaxPerBooking)
            .WithName("Travellers")
            .WithMessage($"At most {Travellers.MaxPerBooking} people per booking. For bigger groups, ask us for a custom trip.");
        RuleFor(x => x.SingleRooms).GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(x => x.Adults).WithMessage("At most one single room per adult.");

        RuleFor(x => x.Nights).InclusiveBetween(1, 59).When(x => x.Nights is not null);
    }
}
