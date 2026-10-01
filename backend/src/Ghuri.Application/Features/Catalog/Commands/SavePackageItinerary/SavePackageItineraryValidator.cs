using FluentValidation;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Catalog.Commands.SavePackageItinerary;

/// <summary>Lengths match the catalog.ItineraryDays columns.</summary>
internal sealed class SavePackageItineraryValidator : AbstractValidator<SavePackageItineraryCommand>
{
    public SavePackageItineraryValidator()
    {
        RuleFor(x => x.Days)
            .Must(days => days is null || days.Count <= PackagePricing.MaxDays)
            .WithMessage($"At most {PackagePricing.MaxDays} days.");

        // Errors come back as e.g. "Days[2].Title", so the form can mark Day 3's title box.
        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.Title).NotEmpty().MaximumLength(200);
            day.RuleFor(d => d.Description).NotEmpty().MaximumLength(4000);
            day.RuleFor(d => d.Meals).MaximumLength(10);
            day.RuleFor(d => d.Accommodation).MaximumLength(150);
        });
    }
}
