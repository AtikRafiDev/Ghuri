using FluentValidation;

namespace Ghuri.Application.Features.Catalog.Commands.CreateDestination;

internal sealed class CreateDestinationValidator : AbstractValidator<CreateDestinationCommand>
{
    public CreateDestinationValidator() => Include(new DestinationFieldsValidator());
}
