using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using BookingEntity = Ghuri.Domain.Entities.Booking.Booking;

namespace Ghuri.Application.Features.Booking.Commands.CreateBooking;

/// <summary>
/// Creates a booking held for 20 minutes while the customer pays.
/// </summary>
/// <remarks>
/// <para>
/// Runs inside ONE transaction (TransactionBehavior). Order matters:
/// (1) lock the Idempotency-Key - a retry gets the first answer;
/// (2) check the trip can be booked - friendly errors, same as the quote;
/// (3) fixed only: reserve the seats with ONE atomic UPDATE - if two
///     customers race for the last seat, exactly one wins;
/// (4) create the booking - Booking works the price out itself;
/// (5) save the answer under the key.
/// </para>
/// <para>
/// Any failure after (3) returns a failed Result, so TransactionBehavior
/// rolls back - and the seat UPDATE is rolled back with everything else.
/// The seats are never "lost" by a half-finished booking.
/// </para>
/// </remarks>
internal sealed class CreateBookingHandler(
    ITourPackageRepository packages,
    IDepartureRepository departures,
    IBookingRepository bookings,
    IIdempotencyStore idempotency,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<CreateBookingCommand, CreateBookingResponse>
{
    public async ValueTask<Result<CreateBookingResponse>> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        // (1) Same key again? Then it's a retry: send back the first answer.
        // The validator guarantees a key.
        var key = command.IdempotencyKey!;
        var requestHash = Fingerprint(command);
        var stored = await idempotency.FindAndLockAsync(key, cancellationToken);
        if (stored is not null)
        {
            return stored.UserId == customerId && stored.RequestHash == requestHash
                ? JsonSerializer.Deserialize<CreateBookingResponse>(stored.ResponseJson)!
                : BookingErrors.IdempotencyKeyReused;
        }

        // Security pass (Day 16): every unpaid booking holds seats for 20
        // minutes, so one account booking over and over without paying could
        // block a whole departure. A few open holds at a time is plenty for a person.
        if (await bookings.CountUnpaidHoldsAsync(customerId, clock.GetUtcNow().UtcDateTime, cancellationToken) >= BookingErrors.MaxUnpaidHolds)
            return BookingErrors.TooManyUnpaidBookings;

        // (2) The package must be for sale.
        var package = await packages.GetPublishedBySlugAsync(Slug.Create(command.PackageSlug), cancellationToken);
        if (package is null)
            return BookingErrors.PackageNotFound;

        var today = clock.Today();
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var travellers = command.Travellers
            .Select(t => new TravellerDetails(t.Type, t.FullName, t.IsLead, t.Gender, t.DateOfBirth, t.Nationality, t.Phone))
            .ToList();
        var contact = new BookingContact(command.ContactName, PhoneNumber.Create(command.ContactPhone), command.ContactEmail);

        var created = package.PricingMode == PricingMode.FixedDepartures
            ? await BookDepartureAsync(command, package, travellers, contact, customerId, today, nowUtc, cancellationToken)
            : await BookFlexibleStayAsync(command, package, travellers, contact, customerId, today, nowUtc, cancellationToken);
        if (created.IsFailure)
            return created.Error; // rolls back - including a seat reservation, if one was made

        var booking = created.Value;
        bookings.Add(booking);

        // (5) Remember the answer, in the same SaveChanges as the booking.
        var response = new CreateBookingResponse(booking.Id, booking.BookingNo, booking.TotalAmount, booking.Currency, booking.HoldExpiresAtUtc!.Value);
        idempotency.Save(key, customerId, requestHash, statusCode: 201, JsonSerializer.Serialize(response), nowUtc);
        return response;
    }

    private async Task<Result<BookingEntity>> BookDepartureAsync(
        CreateBookingCommand command, TourPackage package, IReadOnlyList<TravellerDetails> travellers, BookingContact contact,
        Guid customerId, DateOnly today, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (command.DepartureId is not { } departureId)
            return BookingErrors.DepartureRequired;

        var departure = await departures.GetByIdAsync(departureId, cancellationToken);
        if (departure is null || departure.PackageId != package.Id)
            return BookingErrors.DepartureNotFound;

        // Infants sit on a lap: only adults and children take seats.
        var seats = (short)travellers.Count(t => t.Type != TravellerType.Infant);

        // (2) The same checks and messages as the quote.
        switch (departure.CheckBookable(today, seats))
        {
            case DepartureBookability.NotOpen:
                return BookingErrors.DepartureNotBookable;
            case DepartureBookability.BookingClosed:
                return BookingErrors.BookingClosed(departure.LastBookingDate);
            case DepartureBookability.NotEnoughSeats:
                return BookingErrors.NotEnoughSeats(departure.SeatsLeft);
        }

        // (3) The check above used a moment-old seat count. THIS is the real
        // decision: one UPDATE that only succeeds while the seats are free.
        if (!await departures.TryReserveSeatsAsync(departure.Id, seats, cancellationToken))
            return BookingErrors.SeatsJustTaken;

        // (4)
        return BookingEntity.CreateForDeparture(
            await NextBookingNoAsync(cancellationToken), customerId, package, departure, travellers, contact, command.SpecialRequest,
            BookingSource.Web, today, nowUtc);
    }

    private async Task<Result<BookingEntity>> BookFlexibleStayAsync(
        CreateBookingCommand command, TourPackage package, IReadOnlyList<TravellerDetails> travellers, BookingContact contact,
        Guid customerId, DateOnly today, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (command.StartDate is not { } checkIn || command.Nights is not { } nights)
            return BookingErrors.FlexibleDatesRequired;

        // (2) Same checks and messages as the quote. No seats: a flexible
        // stay is a hotel booking, confirmed by staff within 24 hours.
        switch (package.CheckFlexibleStay(today, checkIn, nights))
        {
            case FlexibleStayBookability.NightsOutOfRange:
                return BookingErrors.NightsOutOfRange(package.MinNights!.Value, package.MaxNights!.Value);
            case FlexibleStayBookability.TooSoon:
                return BookingErrors.StartDateTooSoon(package.EarliestFlexibleStart(today));
        }

        // (4)
        return BookingEntity.CreateFlexible(
            await NextBookingNoAsync(cancellationToken), customerId, package, checkIn, (byte)nights, travellers, contact, command.SpecialRequest,
            BookingSource.Web, today, nowUtc);
    }

    /// <summary>
    /// Taken only once every check has passed: a SEQUENCE never rolls back,
    /// so taking it earlier would waste a number on every "sold out" attempt.
    /// (A later failure can still leave a gap - harmless, numbers just skip.)
    /// </summary>
    private Task<string> NextBookingNoAsync(CancellationToken cancellationToken) =>
        bookings.NextBookingNoAsync(cancellationToken);

    /// <summary>
    /// SHA-256 of the request as JSON (64 hex characters, the column's size).
    /// A retry sends the same body, so it gets the same fingerprint; a
    /// different booking under a reused key does not. The key itself is
    /// left out by its [JsonIgnore].
    /// </summary>
    private static string Fingerprint(CreateBookingCommand command) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command))));
}
