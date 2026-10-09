using Ghuri.Application.Features.Payments.Commands.CheckGatewayRefund;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ghuri.Infrastructure.Jobs;

/// <summary>Settings from appsettings.json "Jobs:RefundStatus".</summary>
internal sealed class RefundStatusOptions
{
    public const string SectionName = "Jobs:RefundStatus";

    /// <summary>Off in the integration tests, which call RunOnceAsync themselves.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// SSLCommerz sends no message when a refund is done, so we ask. Every 15
    /// minutes is plenty for money that takes hours to days to arrive; staff
    /// can press "Check now" for one refund at any time.
    /// </summary>
    public int IntervalSeconds { get; init; } = 900;

    public int BatchSize { get; init; } = 50;
}

/// <summary>
/// Refunds SSLCommerz is sending → asks how each is going, and completes or
/// fails it (CheckGatewayRefundCommand). The same shape as QuoteExpiryJob:
/// find them, then one command per refund, each in its own transaction.
/// </summary>
internal sealed class RefundStatusJob(
    IServiceScopeFactory scopes,
    IOptions<RefundStatusOptions> options,
    ILogger<RefundStatusJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("The refund status job is switched off (Jobs:RefundStatus:Enabled).");
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
                logger.LogError(exception, "The refund status run failed; trying again in {Seconds} seconds.", settings.IntervalSeconds);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>One run: check every refund SSLCommerz is sending. Returns how many were checked.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> refundNos;
        await using (var scope = scopes.CreateAsyncScope())
        {
            refundNos = (await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new GetRefundsWithGatewayQuery(options.Value.BatchSize), cancellationToken)).Value;
        }

        foreach (var refundNo in refundNos)
        {
            try
            {
                // A "no answer" comes back as a failed Result (logged by LoggingBehavior) - the next run asks again.
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CheckGatewayRefundCommand(refundNo), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Refund {RefundNo} could not be checked; the next run will try again.", refundNo);
            }
        }

        return refundNos.Count;
    }
}
