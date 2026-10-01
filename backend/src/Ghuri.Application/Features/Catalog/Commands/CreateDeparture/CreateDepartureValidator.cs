using FluentValidation;

namespace Ghuri.Application.Features.Catalog.Commands.CreateDeparture;

internal sealed class CreateDepartureValidator : AbstractValidator<CreateDepartureCommand>
{
    public CreateDepartureValidator() => Include(new DepartureFieldsValidator());
}
