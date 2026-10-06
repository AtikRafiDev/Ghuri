using FluentValidation;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.CustomTrips.Commands.RejectCustomTrip;

/// <summary>
/// Staff can't do the trip (17-day plan, Day 13: RejectCustomTrip) - no
/// hotels, a closed route, an unsafe season. The reason is emailed to the
/// customer, so write it for them.
/// </summary>
public sealed record RejectCustomTripCommand(string TripNo, string Reason) : ICommand;

internal sealed class RejectCustomTripValidator : AbstractValidator<RejectCustomTripCommand>
{
    public RejectCustomTripValidator()
    {
        RuleFor(x => x.TripNo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

internal sealed class RejectCustomTripHandler(ICustomTripRepository trips, TimeProvider clock) : ICommandHandler<RejectCustomTripCommand>
{
    public async ValueTask<Result> Handle(RejectCustomTripCommand command, CancellationToken cancellationToken)
    {
        var trip = await trips.GetByTripNoForUpdateAsync(command.TripNo, cancellationToken);
        if (trip is null)
            return CustomTripErrors.TripNotFound;
        if (trip.Status is not (CustomTripStatus.Submitted or CustomTripStatus.Quoted or CustomTripStatus.Expired))
            return CustomTripErrors.NotRejectable;

        trip.Reject(command.Reason, clock.GetUtcNow().UtcDateTime);
        return Result.Success();
    }
}
