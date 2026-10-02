using Ghuri.Application.Features.Booking.Commands.ExpireBooking;
using Ghuri.Application.Features.Booking.Queries.GetExpiredHolds;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ghuri.Infrastructure.Jobs;

/// <summary>Settings from appsettings.json "Jobs:BookingExpiry".</summary>
internal sealed class BookingExpiryOptions
{
    public const string SectionName = "Jobs:BookingExpiry";

    /// <summary>Off in the integration tests, which call RunOnceAsync themselves at the moment they choose.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>How often to look for expired holds. Every minute = a seat comes back at most a minute after its 20 minutes are up.</summary>
    public int IntervalSeconds { get; init; } = 60;

    /// <summary>Bookings expired per run at most; any more wait for the next run.</summary>
    public int BatchSize { get; init; } = 100;
}

/// <summary>
/// Gives back the seats of bookings nobody paid for (17-day plan, Day 8:
/// "ExpireUnpaidHolds"). Runs inside the API process: a .NET
/// BackgroundService started with the app - no extra package or database
/// tables (Hangfire can replace it later; the commands stay the same).
/// </summary>
/// <remarks>
/// Each booking is its own ExpireBookingCommand in its own scope, so it
/// gets its own transaction: one booking that fails (e.g. its payment
/// confirmed at that very moment) doesn't stop the others, and is simply
/// looked at again on the next run. If several copies of the API ever run,
/// each runs this job - harmless, an already-expired booking is skipped.
/// </remarks>
internal sealed class BookingExpiryJob(
    IServiceScopeFactory scopes,
    IOptions<BookingExpiryOptions> options,
    ILogger<BookingExpiryJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("The booking expiry job is switched off (Jobs:BookingExpiry:Enabled).");
            return;
        }

        // Once straight away (holds may have run out while the API was
        // stopped), then once per interval until the app shuts down.
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.IntervalSeconds));
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                // E.g. the database is unreachable. An exception escaping a
                // BackgroundService stops the WHOLE app, so it's logged and
                // the job simply tries again next time.
                logger.LogError(exception, "The booking expiry run failed; trying again in {Seconds} seconds.", settings.IntervalSeconds);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>One run: find the expired holds, then expire them one by one. Returns how many were found.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> bookingIds;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            bookingIds = (await sender.Send(new GetExpiredHoldsQuery(options.Value.BatchSize), cancellationToken)).Value;
        }

        foreach (var bookingId in bookingIds)
        {
            try
            {
                // A new scope per booking = a new DbContext and transaction per booking.
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ExpireBookingCommand(bookingId), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Booking {BookingId} could not be expired; the next run will try again.", bookingId);
            }
        }

        return bookingIds.Count;
    }
}
