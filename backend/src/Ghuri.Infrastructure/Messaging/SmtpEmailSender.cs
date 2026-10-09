using Ghuri.Application.Abstractions.Ports;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Ghuri.Infrastructure.Messaging;

/// <summary>Settings from appsettings.json "Email" when Email:Sender is "Smtp".</summary>
internal sealed class SmtpEmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Who the email is from, e.g. bookings@ghuri.com.</summary>
    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = "Ghuri";

    public SmtpServerOptions Smtp { get; init; } = new();
}

/// <summary>
/// "Email:Smtp". Locally smtp4dev: localhost:25, Security None, no login.
/// A real server - Gmail (smtp.gmail.com, 587, StartTls, the Gmail address +
/// an App Password) so emails reach real inboxes - gets its settings from
/// user-secrets (README section 8), or on a server from environment
/// variables (Email__Smtp__Password). Never from git: the repository is public.
/// </summary>
internal sealed class SmtpServerOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 25;

    /// <summary>MailKit's SecureSocketOptions: None, Auto, SslOnConnect, StartTls, StartTlsWhenAvailable.</summary>
    public SecureSocketOptions Security { get; init; } = SecureSocketOptions.StartTls;

    public string? UserName { get; init; }
    public string? Password { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}

/// <summary>
/// Sends real emails over SMTP with MailKit (Day 11) - with attachments
/// (the voucher and invoice PDFs). In development it talks to smtp4dev, a
/// fake mail server that catches everything and shows it at http://localhost:5000.
/// </summary>
/// <remarks>
/// One connection per email: simple and plenty for a booking site (a few
/// emails a minute). Failures throw - the outbox retries confirmation emails;
/// a password-reset request shows the user an error and they try again.
/// </remarks>
internal sealed class SmtpEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var mime = Build(message, settings);

        using var client = new SmtpClient { Timeout = settings.Smtp.TimeoutSeconds * 1000 };
        await client.ConnectAsync(settings.Smtp.Host, settings.Smtp.Port, settings.Smtp.Security, cancellationToken);
        if (!string.IsNullOrEmpty(settings.Smtp.UserName))
            await client.AuthenticateAsync(settings.Smtp.UserName, settings.Smtp.Password ?? string.Empty, cancellationToken);
        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        // The address and subject only - never the body (it can hold a password-reset link).
        logger.LogInformation("Email \"{Subject}\" sent to {To}.", message.Subject, message.To);
    }

    /// <summary>The email as MIME: HTML body plus each attachment. Separate so a test can look at it without a mail server.</summary>
    internal static MimeMessage Build(EmailMessage message, SmtpEmailOptions settings)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        var body = new BodyBuilder { HtmlBody = message.HtmlBody };
        foreach (var file in message.Attachments ?? [])
            body.Attachments.Add(file.FileName, file.Content, ContentType.Parse(file.ContentType));
        mime.Body = body.ToMessageBody();

        return mime;
    }
}
