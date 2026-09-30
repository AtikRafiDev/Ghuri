using FluentValidation;

namespace Ghuri.Application.Features.Catalog.Commands.UpdateCategory;

internal sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator() => Include(new CategoryFieldsValidator());
}
