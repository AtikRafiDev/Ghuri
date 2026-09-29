using FluentValidation;

namespace Ghuri.Application.Features.Identity.Commands.Login;

internal sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.PhoneOrEmail).NotEmpty().MaximumLength(256);

        // Only "not empty" and the maximum - NOT the new-password rules: an
        // existing password must still work even if the rules change later.
        RuleFor(x => x.Password).NotEmpty().MaximumLength(PasswordRules.MaxLength);
    }
}
