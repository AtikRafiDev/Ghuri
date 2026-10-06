using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.CustomTrips.Commands.SubmitCustomTrip;

/// <summary>
/// The customer asks for a multi-destination trip (17-day plan, Day 13:
/// SubmitCustomTrip) - e.g. Cox's Bazar 3 nights → Sylhet 2 nights from
/// 20 Dec. The legs come in order; their dates are worked out here. Staff
/// get an email and send a quote (target: within 24 hours).
/// </summary>
/// <param name="StartDate">The first day, at least CustomTrip.MinLeadDays away.</param>
/// <param name="Adults">At least one.</param>
/// <param name="Children">Under 12.</param>
/// <param name="Infants">Under 2 - at most one per adult.</param>
/// <param name="HotelLevel">Budget, standard or premium - what staff quote for.</param>
/// <param name="Legs">The stops in order. The last one's transfer is ignored (nowhere to go next).</param>
/// <param name="BudgetPerPerson">Optional hint for staff.</param>
/// <param name="Notes">Anything else: "a sea-facing room", "vegetarian meals"…</param>
public sealed record SubmitCustomTripCommand(
    DateOnly StartDate,
    int Adults,
    int Children,
    int Infants,
    HotelLevel HotelLevel,
    decimal? BudgetPerPerson,
    string? Notes,
    IReadOnlyList<SubmitCustomTripLeg> Legs) : ICommand<SubmitCustomTripResponse>;

public sealed record SubmitCustomTripLeg(Guid DestinationId, int Nights, TransferMode TransferToNext);

public sealed record SubmitCustomTripResponse(string TripNo, DateOnly StartDate, DateOnly EndDate, int TotalNights);

internal sealed class SubmitCustomTripValidator : AbstractValidator<SubmitCustomTripCommand>
{
    public SubmitCustomTripValidator()
    {
        RuleFor(x => x.Adults).InclusiveBetween(1, Travellers.MaxPerBooking);
        RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Infants).GreaterThanOrEqualTo(0).LessThanOrEqualTo(x => x.Adults)
            .WithMessage("Each infant needs an adult to travel with.");
        RuleFor(x => x.Adults + x.Children + x.Infants).LessThanOrEqualTo(Travellers.MaxPerBooking)
            .OverridePropertyName("adults")
            .WithMessage($"At most {Travellers.MaxPerBooking} people - for bigger groups, please call us.");
        RuleFor(x => x.HotelLevel).IsInEnum();
        RuleFor(x => x.BudgetPerPerson).GreaterThan(0).When(x => x.BudgetPerPerson is not null);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.Legs).NotEmpty().WithMessage(CustomTripErrors.LegsMessage)
            .Must(legs => legs is null || legs.Count <= CustomTrip.MaxLegs).WithMessage(CustomTripErrors.LegsMessage);
        RuleForEach(x => x.Legs).ChildRules(leg =>
        {
            leg.RuleFor(l => l.DestinationId).NotEmpty().WithMessage("Choose a destination.");
            leg.RuleFor(l => l.Nights).InclusiveBetween(1, CustomTrip.MaxNightsPerLeg);
            leg.RuleFor(l => l.TransferToNext).IsInEnum();
        });
        RuleFor(x => x.Legs.Sum(l => l.Nights)).LessThanOrEqualTo(CustomTrip.MaxTotalNights)
            .When(x => x.Legs is not null)
            .OverridePropertyName("legs")
            .WithMessage($"A custom trip can be at most {CustomTrip.MaxTotalNights} nights.");
    }
}

/// <remarks>
/// The contact (name, mobile, email) comes from the customer's own account -
/// the quote email goes there. Every destination must exist; the domain
/// works out the dates and raises CustomTripSubmitted (→ the two emails).
/// </remarks>
internal sealed class SubmitCustomTripHandler(
    ICustomTripRepository trips,
    IReadDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<SubmitCustomTripCommand, SubmitCustomTripResponse>
{
    public async ValueTask<Result<SubmitCustomTripResponse>> Handle(SubmitCustomTripCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        var customer = await db.Users
            .Where(u => u.Id == customerId)
            .Select(u => new { u.FullName, u.PhoneNumber, u.Email })
            .FirstOrDefaultAsync(cancellationToken);
        if (customer is null)
            return IdentityErrors.NotAuthenticated;

        var today = clock.Today();
        if (command.StartDate < today.AddDays(CustomTrip.MinLeadDays))
            return CustomTripErrors.StartTooSoon(today.AddDays(CustomTrip.MinLeadDays));

        // Deleted destinations are filtered out by the soft-delete query filter.
        var wanted = command.Legs.Select(l => l.DestinationId).Distinct().ToList();
        var found = await db.Destinations.CountAsync(d => wanted.Contains(d.Id), cancellationToken);
        if (found != wanted.Count)
            return CustomTripErrors.DestinationNotFound;

        var trip = CustomTrip.Submit(
            await trips.NextTripNoAsync(cancellationToken), customerId, command.StartDate,
            Travellers.Create(command.Adults, command.Children, command.Infants), command.HotelLevel,
            command.BudgetPerPerson, command.Notes,
            new BookingContact(customer.FullName, customer.PhoneNumber, customer.Email),
            command.Legs.Select(l => new CustomTripLegRequest(l.DestinationId, l.Nights, l.TransferToNext)).ToList(),
            today, clock.GetUtcNow().UtcDateTime);
        trips.Add(trip);

        return new SubmitCustomTripResponse(trip.TripNo, trip.StartDate, trip.EndDate, trip.TotalNights);
    }
}
