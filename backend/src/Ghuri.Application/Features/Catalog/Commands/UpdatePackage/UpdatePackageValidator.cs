using FluentValidation;

namespace Ghuri.Application.Features.Catalog.Commands.UpdatePackage;

internal sealed class UpdatePackageValidator : AbstractValidator<UpdatePackageCommand>
{
    public UpdatePackageValidator() => Include(new PackageFieldsValidator());
}
