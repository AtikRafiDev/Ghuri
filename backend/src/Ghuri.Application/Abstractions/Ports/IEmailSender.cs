namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// Sends an email. Which provider (SMTP, SendGrid, or "just write it to
/// the log" in development) is decided by configuration in
/// Infrastructure - handlers never know or care.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// One email. A record rather than separate parameters, so attachments
/// (the Day 11 invoice PDF) can be added later without changing every caller.
/// </summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody);
