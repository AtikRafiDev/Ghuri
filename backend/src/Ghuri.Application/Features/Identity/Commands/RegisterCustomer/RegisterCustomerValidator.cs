using FluentValidation;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Identity.Commands.RegisterCustomer;

internal sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);

        // The Domain's own rule, reused - not a second copy of "what a BD
        // mobile number looks like" that could drift from PhoneNumber.
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Must(PhoneNumber.IsValid)
            .WithMessage("Enter a Bangladeshi mobile number, e.g. 01711000000.");

        // Email is optional for customers (blueprint: iam.Users.Email NULL).
        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Password).ValidNewPassword();
    }
}
