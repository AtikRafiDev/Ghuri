using Ghuri.Application.Features.CustomTrips.Commands.ExpireCustomTripQuote;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ghuri.Infrastructure.Jobs;

/// <summary>Settings from appsettings.json "Jobs:QuoteExpiry".</summary>
internal sealed class QuoteExpiryOptions
{
    public const string SectionName = "Jobs:QuoteExpiry";

    /// <summary>Off in the integration tests, which call RunOnceAsync themselves.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>The plan says hourly: a quote valid "3 days" may live up to an hour longer - harmless, the customer page already shows it as expired.</summary>
    public int IntervalSeconds { get; init; } = 3600;

    public int BatchSize { get; init; } = 100;
}

/// <summary>
/// Custom-trip quotes nobody accepted in time → Expired (17-day plan,
/// Day 13: "ExpireQuotes (hourly job)"). The same shape as BookingExpiryJob:
/// find them, then one command per trip, each in its own transaction.
/// </summary>
internal sealed class QuoteExpiryJob(
    IServiceScopeFactory scopes,
    IOptions<QuoteExpiryOptions> options,
    ILogger<QuoteExpiryJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("The quote expiry job is switched off (Jobs:QuoteExpiry:Enabled).");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.IntervalSeconds));
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "The quote expiry run failed; trying again in {Seconds} seconds.", settings.IntervalSeconds);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>One run: expire every overdue quote. Returns how many were found.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> tripIds;
        await using (var scope = scopes.CreateAsyncScope())
        {
            tripIds = (await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new GetOverdueQuotesQuery(options.Value.BatchSize), cancellationToken)).Value;
        }

        foreach (var tripId in tripIds)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ExpireCustomTripQuoteCommand(tripId), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Quote of custom trip {TripId} could not be expired; the next run will try again.", tripId);
            }
        }

        if (tripIds.Count > 0)
            logger.LogInformation("{Count} custom-trip quote(s) expired.", tripIds.Count);
        return tripIds.Count;
    }
}
