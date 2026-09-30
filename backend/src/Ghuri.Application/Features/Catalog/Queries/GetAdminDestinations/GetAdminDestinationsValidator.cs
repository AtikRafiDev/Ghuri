using FluentValidation;
using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminDestinations;

internal sealed class GetAdminDestinationsValidator : AbstractValidator<GetAdminDestinationsQuery>
{
    public GetAdminDestinationsValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paged<AdminDestinationDto>.MaxPageSize);
    }
}
