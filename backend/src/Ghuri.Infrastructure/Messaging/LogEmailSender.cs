using System.Net;
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
        // The body is HTML, where a link's "&" is written "&amp;". A person
        // copying the link from the console would paste "&amp;token=..." and
        // the page would find no token - so show it decoded, as a mail
        // program would. (Safe: the console never renders HTML.)
        var readableBody = WebUtility.HtmlDecode(message.HtmlBody);

        logger.LogInformation(
            "DEVELOPMENT EMAIL - not really sent{NewLine}To: {To}{NewLine}Subject: {Subject}{NewLine}{Body}",
            Environment.NewLine, message.To, Environment.NewLine, message.Subject, Environment.NewLine, readableBody);

        return Task.CompletedTask;
    }
}
