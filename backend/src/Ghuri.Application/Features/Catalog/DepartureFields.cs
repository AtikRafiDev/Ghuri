using FluentValidation;
using Ghuri.Domain.Entities.Catalog;

namespace Ghuri.Application.Features.Catalog;

/// <summary>
/// The fields of the departure form, shared by Create and Update. There's
/// no EndDate: it is worked out from the package's duration.
/// </summary>
public interface IDepartureFields
{
    /// <summary>JSON "2026-12-20".</summary>
    DateOnly StartDate { get; }

    decimal AdultPrice { get; }
    decimal ChildPrice { get; }
    decimal InfantPrice { get; }

    /// <summary>Extra for one person who wants a room alone. Null = not offered.</summary>
    decimal? SingleSupplement { get; }

    int TotalSeats { get; }
    int BookingCutoffDays { get; }
}

/// <summary>The same limits as Departure itself and the catalog.Departures columns.</summary>
internal sealed class DepartureFieldsValidator : AbstractValidator<IDepartureFields>
{
    public DepartureFieldsValidator()
    {
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Choose the start date.");
        RuleFor(x => x.AdultPrice).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.ChildPrice).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.InfantPrice).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.SingleSupplement).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.TotalSeats).InclusiveBetween(1, Departure.MaxSeats);
        RuleFor(x => x.BookingCutoffDays).InclusiveBetween(0, 60);
    }
}
