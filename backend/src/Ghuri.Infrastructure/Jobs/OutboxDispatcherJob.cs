using System.Reflection;
using System.Text.Json;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Domain.Common;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ghuri.Infrastructure.Jobs;

/// <summary>Settings from appsettings.json "Jobs:Outbox".</summary>
internal sealed class OutboxOptions
{
    public const string SectionName = "Jobs:Outbox";

    /// <summary>Off in the integration tests, which call RunOnceAsync themselves.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>How often to look for new events. 10 seconds = the voucher email leaves at most ~10 s after payment.</summary>
    public int IntervalSeconds { get; init; } = 10;

    /// <summary>Events handled per run at most; any more wait for the next run.</summary>
    public int BatchSize { get; init; } = 20;

    /// <summary>After this many failures an event is left alone (its Error column says why) - a broken handler mustn't retry forever.</summary>
    public byte MaxAttempts { get; init; } = 10;

    /// <summary>
    /// The wait before the first retry; each further retry waits twice as long
    /// (10 s, 20 s, 40 s... with 10 attempts ≈ 2.8 hours in all) - so a mail
    /// server that is down for a while doesn't use up every attempt in a minute.
    /// 0 in the tests: retry at once.
    /// </summary>
    public int RetryDelaySeconds { get; init; } = 10;
}

/// <summary>
/// The read half of the transactional outbox: every few seconds, takes the
/// unhandled ops.OutboxMessages (written by OutboxInterceptor), turns each
/// back into its domain event and gives it to that event's
/// IDomainEventHandler(s) - e.g. BookingConfirmed → the voucher email.
/// </summary>
/// <remarks>
/// <para>
/// Each event gets its own scope and its own save: one failing email
/// (mail server down) doesn't stop the others, and is retried next run, up
/// to MaxAttempts. "At least once": if the email went out but marking the
/// row failed, the email goes out again - acceptable; a lost voucher isn't.
/// </para>
/// <para>
/// One copy of the API is assumed. With several, two could pick up the same
/// row and send an email twice - a row lock (READPAST) would fix that then.
/// </para>
/// </remarks>
internal sealed class OutboxDispatcherJob(
    IServiceScopeFactory scopes,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxDispatcherJob> logger) : BackgroundService
{
    private static readonly MethodInfo HandleMethod =
        typeof(OutboxDispatcherJob).GetMethod(nameof(HandleAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("The outbox dispatcher is switched off (Jobs:Outbox:Enabled).");
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
                // Same as BookingExpiryJob: an escaping exception would stop the whole app.
                logger.LogError(exception, "The outbox run failed; trying again in {Seconds} seconds.", settings.IntervalSeconds);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>One run: handle the oldest pending events. Returns how many were handled successfully.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        List<Guid> pending;
        await using (var scope = scopes.CreateAsyncScope())
        {
            // New events first (Attempts 0), then the retries. Whether a retry
            // is due yet is checked here in memory: only a handful of rows are
            // ever pending, and the rule is easier to read in C# than in SQL.
            var candidates = await scope.ServiceProvider.GetRequiredService<AppDbContext>().OutboxMessages
                .Where(m => m.ProcessedAtUtc == null && m.Attempts < settings.MaxAttempts)
                .OrderBy(m => m.Attempts).ThenBy(m => m.OccurredAtUtc)
                .Take(settings.BatchSize * 5)
                .Select(m => new { m.Id, m.Attempts, m.OccurredAtUtc })
                .ToListAsync(cancellationToken);

            pending = candidates
                .Where(m => nowUtc >= m.OccurredAtUtc + RetryWait(m.Attempts, settings.RetryDelaySeconds))
                .Take(settings.BatchSize)
                .Select(m => m.Id)
                .ToList();
        }

        var handled = 0;
        foreach (var id in pending)
        {
            if (await DispatchOneAsync(id, cancellationToken))
                handled++;
        }

        return handled;
    }

    private async Task<bool> DispatchOneAsync(Guid messageId, CancellationToken cancellationToken)
    {
        // A new scope per event = its own DbContext, like one request.
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = await db.OutboxMessages.SingleAsync(m => m.Id == messageId, cancellationToken);

        bool succeeded;
        try
        {
            var eventType = typeof(IDomainEvent).Assembly.GetType(message.Type)
                            ?? throw new InvalidOperationException($"Unknown event type '{message.Type}'.");
            var domainEvent = JsonSerializer.Deserialize(message.PayloadJson, eventType)
                              ?? throw new InvalidOperationException($"Event {message.Id} has an empty payload.");

            await (Task)HandleMethod.MakeGenericMethod(eventType).Invoke(null, [scope.ServiceProvider, domainEvent, cancellationToken])!;
            message.MarkProcessed(clock.GetUtcNow().UtcDateTime);
            succeeded = true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            message.RecordFailure($"{cause.GetType().Name}: {cause.Message}");
            logger.LogWarning(cause, "Outbox event {Type} ({Id}) failed (attempt {Attempt}).", message.Type, message.Id, message.Attempts);
            succeeded = false;
        }

        await db.SaveChangesAsync(cancellationToken);
        return succeeded;
    }

    /// <summary>
    /// How long after the event a try number (attempts so far) may start:
    /// 0 → at once · 1 → 10 s · 2 → 30 s · 3 → 70 s... (each gap doubles).
    /// Counted from OccurredAtUtc, so no extra "last tried at" column is needed.
    /// </summary>
    internal static TimeSpan RetryWait(byte attempts, int retryDelaySeconds) =>
        TimeSpan.FromSeconds(retryDelaySeconds * (Math.Pow(2, attempts) - 1));

    /// <summary>Every handler of this event type, one after the other.</summary>
    private static async Task HandleAsync<TEvent>(IServiceProvider services, TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        foreach (var handler in services.GetServices<IDomainEventHandler<TEvent>>())
            await handler.HandleAsync(domainEvent, cancellationToken);
    }
}
