using System.Diagnostics;
using Ghuri.Application.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Ghuri.Application.Behaviors;

/// <summary>
/// Logs every use case's name, outcome and duration - once, here, instead
/// of a log line copy-pasted into ~100 handlers.
/// </summary>
/// <remarks>
/// Deliberately logs the message TYPE name only, never its contents. A
/// command's fields can include passwords, phone numbers, passport numbers
/// - the blueprint's rule is "no tokens, passwords or passport numbers in
/// logs", and logging only the name makes that impossible to break by
/// accident.
/// </remarks>
public sealed class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : IResult
{
    public async ValueTask<TResponse> Handle(
        TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TMessage).Name;
        var started = Stopwatch.GetTimestamp();

        logger.LogInformation("Handling {MessageName}", name);

        var response = await next(message, cancellationToken);
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (response.IsFailure)
        {
            logger.LogWarning(
                "{MessageName} failed with {ErrorCode} in {ElapsedMs:0} ms", name, response.Error.Code, elapsedMs);
        }
        else
        {
            logger.LogInformation("Handled {MessageName} in {ElapsedMs:0} ms", name, elapsedMs);
        }

        return response;
    }
}
