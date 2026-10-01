using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.UpdateDeparture;

/// <summary>Replaces a departure's date, prices and seats. Id comes from the URL (PUT /admin/departures/{id}).</summary>
public sealed record UpdateDepartureCommand(
    Guid Id,
    DateOnly StartDate,
    decimal AdultPrice,
    decimal ChildPrice,
    decimal InfantPrice,
    decimal? SingleSupplement,
    int TotalSeats,
    int BookingCutoffDays) : ICommand, IDepartureFields;
