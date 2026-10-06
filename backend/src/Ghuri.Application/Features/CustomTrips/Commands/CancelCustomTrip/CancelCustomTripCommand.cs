using FluentValidation;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.CustomTrips.Commands.CancelCustomTrip;

/// <summary>
/// The customer withdraws their own request (17-day plan, Day 13:
/// CancelCustomTrip) - before accepting a quote. After accepting, it's a
/// booking, and is cancelled like one (with the refund policy).
/// </summary>
public sealed record CancelCustomTripCommand(string TripNo, string? Reason) : ICommand;

internal sealed class CancelCustomTripValidator : AbstractValidator<CancelCustomTripCommand>
{
    public CancelCustomTripValidator()
    {
        RuleFor(x => x.TripNo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

internal sealed class CancelCustomTripHandler(ICustomTripRepository trips, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<CancelCustomTripCommand>
{
    public async ValueTask<Result> Handle(CancelCustomTripCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        // Someone else's trip is "not found", like a booking.
        var trip = await trips.GetByTripNoForUpdateAsync(command.TripNo, cancellationToken);
        if (trip is null || trip.CustomerId != customerId)
            return CustomTripErrors.TripNotFound;
        if (trip.Status is not (CustomTripStatus.Submitted or CustomTripStatus.Quoted or CustomTripStatus.Expired))
            return CustomTripErrors.NotCancellable;

        trip.Cancel(command.Reason, clock.GetUtcNow().UtcDateTime);
        return Result.Success();
    }
}
