using FluentValidation;

namespace Ghuri.Application.Features.Identity.Commands.Logout;

internal sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken).MaximumLength(200);
    }
}
