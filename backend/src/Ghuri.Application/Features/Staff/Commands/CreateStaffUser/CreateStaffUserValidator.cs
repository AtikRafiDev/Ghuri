using FluentValidation;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Staff.Commands.CreateStaffUser;

internal sealed class CreateStaffUserValidator : AbstractValidator<CreateStaffUserCommand>
{
    public CreateStaffUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Must(PhoneNumber.IsValid)
            .WithMessage("Enter a Bangladeshi mobile number, e.g. 01711000000.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Staff need an email address - the welcome link is sent there.")
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Role).Must(User.AssignableStaffRoles.Contains).WithMessage("Choose Manager, Sales or Accounts.");
    }
}
