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

        // A PDF can't be shown in a console - just say what would be attached.
        var attachments = message.Attachments is { Count: > 0 } files
            ? string.Join(", ", files.Select(f => $"{f.FileName} ({f.Content.Length / 1024.0:0.#} KB)"))
            : "none";

        logger.LogInformation(
            "DEVELOPMENT EMAIL - not really sent{NewLine}To: {To}{NewLine}Subject: {Subject}{NewLine}Attachments: {Attachments}{NewLine}{Body}",
            Environment.NewLine, message.To, Environment.NewLine, message.Subject, Environment.NewLine, attachments, Environment.NewLine, readableBody);

        return Task.CompletedTask;
    }
}
