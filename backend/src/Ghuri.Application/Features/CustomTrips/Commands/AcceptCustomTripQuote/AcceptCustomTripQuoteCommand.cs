using FluentValidation;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Application.Features.CustomTrips.Commands.AcceptCustomTripQuote;

/// <summary>
/// The customer takes the quote (17-day plan, Day 15: AcceptQuote): the trip
/// becomes Accepted and a booking is made from it - type CustomTrip, total =
/// the quote, held 20 minutes for payment. From there it's paid exactly like
/// a package booking (InitiatePayment, SSLCommerz, the IPN - unchanged).
/// </summary>
/// <param name="TripNo">e.g. CT1001.</param>
/// <param name="Travellers">Everyone travelling, by name (decided 2026-10-06) - exactly the trip's adults, children and infants; one adult is the lead.</param>
/// <param name="SpecialRequest">Optional, shown to staff on the booking.</param>
public sealed record AcceptCustomTripQuoteCommand(string TripNo, IReadOnlyList<AcceptTraveller> Travellers, string? SpecialRequest)
    : ICommand<AcceptCustomTripQuoteResponse>;

public sealed record AcceptTraveller(TravellerType Type, string FullName, bool IsLead);

/// <summary>Where to pay: the booking's number, its total, and when its hold ends.</summary>
public sealed record AcceptCustomTripQuoteResponse(string BookingNo, decimal TotalAmount, string Currency, DateTime HoldExpiresAtUtc);

internal sealed class AcceptCustomTripQuoteValidator : AbstractValidator<AcceptCustomTripQuoteCommand>
{
    public AcceptCustomTripQuoteValidator()
    {
        RuleFor(x => x.TripNo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Travellers).NotEmpty()
            .Must(t => t is null || t.Count(x => x.IsLead) == 1).WithMessage("Mark exactly one traveller as the lead.");
        RuleForEach(x => x.Travellers).ChildRules(t =>
        {
            t.RuleFor(x => x.Type).IsInEnum();
            t.RuleFor(x => x.FullName).NotEmpty().WithMessage("Enter the traveller's full name.").MaximumLength(150);
        });
        RuleFor(x => x.SpecialRequest).MaximumLength(1000);
    }
}

/// <remarks>
/// <para>
/// The trip row is LOCKED first: two clicks of "Accept" run one after the
/// other, and the second gets the booking the first one made (it's still
/// waiting for payment) - never two bookings for one quote.
/// </para>
/// <para>
/// If an earlier acceptance's 20 minutes ran out but the expiry job hasn't
/// run yet, that booking is expired here first, then the quote is accepted
/// again - the customer doesn't have to wait a minute for the job.
/// </para>
/// </remarks>
internal sealed class AcceptCustomTripQuoteHandler(
    ICustomTripRepository trips,
    IBookingRepository bookings,
    IReadDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<AcceptCustomTripQuoteCommand, AcceptCustomTripQuoteResponse>
{
    public async ValueTask<Result<AcceptCustomTripQuoteResponse>> Handle(AcceptCustomTripQuoteCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        var trip = await trips.GetByTripNoForUpdateAsync(command.TripNo, cancellationToken);
        if (trip is null || trip.CustomerId != customerId)
            return CustomTripErrors.TripNotFound;

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        if (trip.Status == CustomTripStatus.Accepted)
        {
            var pending = await PendingBookingAsync(trip.Id, cancellationToken);
            if (pending is not null && pending.IsAwaitingPayment(nowUtc))
                return Answer(pending); // a second click - the same booking

            // Its hold ran out a moment ago and the expiry job hasn't run: do its work now.
            if (pending is not null)
                pending.Expire(nowUtc);
            trip.ReleaseAcceptance(nowUtc);
        }

        if (trip.Status == CustomTripStatus.Expired || trip.IsQuoteOverdue(nowUtc))
            return CustomTripErrors.QuoteExpired;
        if (trip.Status != CustomTripStatus.Quoted)
            return CustomTripErrors.NotAcceptable;
        if (!MatchesTrip(command.Travellers, trip))
            return CustomTripErrors.TravellersMismatch(trip);

        trip.Accept(nowUtc);
        var booking = BookingEntity.CreateForCustomTrip(
            await bookings.NextBookingNoAsync(cancellationToken), customerId, trip,
            command.Travellers.Select(t => new TravellerDetails(t.Type, t.FullName, t.IsLead)).ToList(),
            new BookingContact(trip.ContactName, trip.ContactPhone, trip.ContactEmail),
            command.SpecialRequest, BookingSource.Web, nowUtc);
        bookings.Add(booking);

        return Answer(booking);
    }

    /// <summary>The booking made by an earlier "Accept" that is still PendingPayment - tracked and locked.</summary>
    private async Task<BookingEntity?> PendingBookingAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var id = await db.Bookings
            .Where(b => b.CustomTripId == tripId && b.Status == BookingStatus.PendingPayment)
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return id is { } bookingId ? await bookings.GetByIdForUpdateAsync(bookingId, cancellationToken) : null;
    }

    private static bool MatchesTrip(IReadOnlyList<AcceptTraveller> travellers, CustomTrip trip) =>
        travellers.Count(t => t.Type == TravellerType.Adult) == trip.Adults
        && travellers.Count(t => t.Type == TravellerType.Child) == trip.Children
        && travellers.Count(t => t.Type == TravellerType.Infant) == trip.Infants
        && travellers.Single(t => t.IsLead).Type == TravellerType.Adult;

    private static AcceptCustomTripQuoteResponse Answer(BookingEntity booking) =>
        new(booking.BookingNo, booking.TotalAmount, booking.Currency, booking.HoldExpiresAtUtc!.Value);
}
