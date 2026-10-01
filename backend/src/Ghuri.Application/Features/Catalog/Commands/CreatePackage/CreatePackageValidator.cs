using FluentValidation;

namespace Ghuri.Application.Features.Catalog.Commands.CreatePackage;

internal sealed class CreatePackageValidator : AbstractValidator<CreatePackageCommand>
{
    public CreatePackageValidator() => Include(new PackageFieldsValidator());
}
