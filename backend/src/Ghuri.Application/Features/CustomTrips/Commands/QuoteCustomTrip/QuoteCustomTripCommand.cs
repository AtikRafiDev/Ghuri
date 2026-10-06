using FluentValidation;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.CustomTrips.Commands.QuoteCustomTrip;

/// <summary>
/// Staff price a custom trip (17-day plan, Day 13: QuoteCustomTrip): the
/// day-by-day plan, the price lines (hotel, transport, meals…) and how many
/// days the offer stands (3 by default). Sending it again REPLACES the
/// previous quote and restarts the clock - allowed while the trip is
/// Submitted, Quoted or Expired (decided 2026-10-06). The customer is emailed.
/// </summary>
/// <param name="TripNo">e.g. CT1001.</param>
/// <param name="Itinerary">The plan, day by day, as the customer will read it.</param>
/// <param name="Lines">The price lines; their sum is what the customer pays.</param>
/// <param name="ValidDays">Null = CustomTrip.DefaultQuoteValidDays (3).</param>
public sealed record QuoteCustomTripCommand(string TripNo, string Itinerary, IReadOnlyList<QuoteCustomTripLine> Lines, int? ValidDays)
    : ICommand<QuoteCustomTripResponse>;

public sealed record QuoteCustomTripLine(QuoteLineCategory Category, string Description, decimal Amount);

public sealed record QuoteCustomTripResponse(string TripNo, int QuoteVersion, decimal Total, DateTime ExpiresAtUtc);

internal sealed class QuoteCustomTripValidator : AbstractValidator<QuoteCustomTripCommand>
{
    public QuoteCustomTripValidator()
    {
        RuleFor(x => x.TripNo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Itinerary).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one price line.")
            .Must(lines => lines is null || lines.Count <= CustomTrip.MaxQuoteLines)
            .WithMessage($"At most {CustomTrip.MaxQuoteLines} price lines.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Category).IsInEnum();
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(200);
            line.RuleFor(l => l.Amount).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        });
        RuleFor(x => x.ValidDays).InclusiveBetween(1, CustomTrip.MaxQuoteValidDays).When(x => x.ValidDays is not null);
    }
}

internal sealed class QuoteCustomTripHandler(ICustomTripRepository trips, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<QuoteCustomTripCommand, QuoteCustomTripResponse>
{
    public async ValueTask<Result<QuoteCustomTripResponse>> Handle(QuoteCustomTripCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } staffId)
            return IdentityErrors.NotAuthenticated;

        // Locked: the expiry job or the customer cancelling at this moment waits for us.
        var trip = await trips.GetByTripNoForUpdateAsync(command.TripNo, cancellationToken);
        if (trip is null)
            return CustomTripErrors.TripNotFound;
        if (trip.Status is not (CustomTripStatus.Submitted or CustomTripStatus.Quoted or CustomTripStatus.Expired))
            return CustomTripErrors.NotQuotable;

        trip.Quote(
            command.Itinerary,
            command.Lines.Select(l => new QuoteLineInput(l.Category, l.Description, l.Amount)).ToList(),
            command.ValidDays ?? CustomTrip.DefaultQuoteValidDays,
            staffId,
            clock.GetUtcNow().UtcDateTime);

        return new QuoteCustomTripResponse(trip.TripNo, trip.QuoteVersion, trip.QuoteTotal!.Value, trip.QuoteExpiresAtUtc!.Value);
    }
}
