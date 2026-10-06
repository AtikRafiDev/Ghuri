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
/// (the Day 11 invoice PDF) could be added without changing every caller.
/// </summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, IReadOnlyList<EmailAttachment>? Attachments = null);

/// <summary>A file sent with an email, e.g. ("Invoice-TB100001.pdf", "application/pdf", bytes).</summary>
public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);
