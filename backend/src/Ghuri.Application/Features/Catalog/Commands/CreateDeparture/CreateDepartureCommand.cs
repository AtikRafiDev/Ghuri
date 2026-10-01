using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.CreateDeparture;

/// <summary>Adds a dated departure to a fixed-departure package. Returns its id. PackageId comes from the URL.</summary>
public sealed record CreateDepartureCommand(
    Guid PackageId,
    DateOnly StartDate,
    decimal AdultPrice,
    decimal ChildPrice,
    decimal InfantPrice,
    decimal? SingleSupplement,
    int TotalSeats,
    int BookingCutoffDays) : ICommand<Guid>, IDepartureFields;
