using FluentValidation;

namespace Ghuri.Application.Features.Identity.Commands.RefreshSession;

internal sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionValidator()
    {
        // Real tokens are 86 characters; anything far longer is junk.
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}
