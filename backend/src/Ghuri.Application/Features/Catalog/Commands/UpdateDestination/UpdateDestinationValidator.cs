using FluentValidation;

namespace Ghuri.Application.Features.Catalog.Commands.UpdateDestination;

internal sealed class UpdateDestinationValidator : AbstractValidator<UpdateDestinationCommand>
{
    public UpdateDestinationValidator() => Include(new DestinationFieldsValidator());
}
