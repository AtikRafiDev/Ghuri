using FluentValidation;

namespace Ghuri.Application.Features.Catalog.Commands.CreateCategory;

internal sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator() => Include(new CategoryFieldsValidator());
}
