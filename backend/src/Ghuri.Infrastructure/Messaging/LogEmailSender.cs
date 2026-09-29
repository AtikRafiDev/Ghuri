using Ghuri.Application.Abstractions.Ports;
using Microsoft.Extensions.Logging;

namespace Ghuri.Infrastructure.Messaging;

/// <summary>
/// DEVELOPMENT ONLY: instead of sending, writes the whole email - reset
/// link included - to the API's console. No mail server, no Docker, no
/// real inbox needed to test "Forgot password".
/// </summary>
/// <remarks>
/// Chosen with "Email:Sender": "Log" in appsettings.Development.json.
/// Never use it on a real server: the log would then contain working
/// password-reset links, which the blueprint forbids ("never log tokens").
/// A real SMTP/SendGrid sender implements the same IEmailSender later.
/// </remarks>
internal sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "DEVELOPMENT EMAIL - not really sent{NewLine}To: {To}{NewLine}Subject: {Subject}{NewLine}{Body}",
            Environment.NewLine, message.To, Environment.NewLine, message.Subject, Environment.NewLine, message.HtmlBody);

        return Task.CompletedTask;
    }
}
