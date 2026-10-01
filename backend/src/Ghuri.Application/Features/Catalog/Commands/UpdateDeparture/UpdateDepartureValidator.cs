using FluentValidation;

namespace Ghuri.Application.Features.Catalog.Commands.UpdateDeparture;

internal sealed class UpdateDepartureValidator : AbstractValidator<UpdateDepartureCommand>
{
    public UpdateDepartureValidator() => Include(new DepartureFieldsValidator());
}
