using FluentValidation;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Booking.Commands.CreateBooking;

/// <summary>
/// The form's rules, as field errors the checkout can show next to each box.
/// Booking itself checks the same traveller rules again (the domain is the
/// last line of defence) - these exist so the customer gets a clear message
/// instead of a generic error.
/// </summary>
internal sealed class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator(TimeProvider clock)
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Send an Idempotency-Key header - a new random id (e.g. a UUID) for each checkout.")
            .MaximumLength(80) // ops.IdempotencyKeys.Key
            .Matches("^[A-Za-z0-9_.:-]+$").WithMessage("The Idempotency-Key may only use letters, digits and _ . : -");

        RuleFor(x => x.PackageSlug).NotEmpty().MaximumLength(220);

        RuleFor(x => x.Nights).InclusiveBetween(1, 59).When(x => x.Nights is not null);

        RuleFor(x => x.Travellers)
            .NotEmpty().WithMessage("Add the travellers.")
            .Must(t => t.Count <= Travellers.MaxPerBooking)
            .WithMessage($"At most {Travellers.MaxPerBooking} people per booking. For bigger groups, ask us for a custom trip.")
            .Must(t => t.Count(p => p.IsLead) == 1).WithMessage("Mark exactly one traveller as the lead traveller.")
            .Must(t => t.Where(p => p.IsLead).All(p => p.Type == TravellerType.Adult)).WithMessage("The lead traveller must be an adult.")
            .Must(t => t.Count(p => p.Type == TravellerType.Infant) <= t.Count(p => p.Type == TravellerType.Adult))
            .WithMessage("Each infant needs an adult to travel with.");

        RuleForEach(x => x.Travellers).ChildRules(traveller =>
        {
            traveller.RuleFor(t => t.Type).IsInEnum();
            traveller.RuleFor(t => t.FullName).NotEmpty().WithMessage("Enter the traveller's full name.").MaximumLength(150);
            traveller.RuleFor(t => t.Gender).IsInEnum().When(t => t.Gender is not null);
            traveller.RuleFor(t => t.DateOfBirth)
                .LessThanOrEqualTo(_ => clock.Today()).WithMessage("The date of birth can't be in the future.")
                .When(t => t.DateOfBirth is not null);
            traveller.RuleFor(t => t.Nationality)
                .Matches("^[A-Za-z]{2}$").WithMessage("Use the two-letter country code, e.g. BD.")
                .When(t => !string.IsNullOrWhiteSpace(t.Nationality));
            traveller.RuleFor(t => t.Phone).MaximumLength(20);
        });

        RuleFor(x => x.ContactName).NotEmpty().WithMessage("Enter the contact person's name.").MaximumLength(150);
        RuleFor(x => x.ContactPhone)
            .Must(PhoneNumber.IsValid).WithMessage("Enter a valid mobile number, e.g. 01712345678.");
        RuleFor(x => x.ContactEmail)
            .EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.SpecialRequest).MaximumLength(1000);
    }
}
