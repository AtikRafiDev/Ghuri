using Ghuri.Application.Abstractions.Ports;
using Ghuri.Infrastructure.Messaging;
using MimeKit;

namespace Ghuri.Api.IntegrationTests.Documents;

/// <summary>The email as MailKit builds it - checked without any mail server.</summary>
public class SmtpEmailSenderTests
{
    private static readonly SmtpEmailOptions Settings = new() { FromAddress = "bookings@ghuri.local", FromName = "Ghuri" };

    [Fact]
    public void TheEmail_HasTheSenderTheBody_AndEveryAttachment()
    {
        var message = new EmailMessage(
            "rahim@example.com", "Booking confirmed - TB100001", "<p>Hi Rahim</p>",
            [
                new EmailAttachment("Voucher-TB100001.pdf", "application/pdf", [0x25, 0x50, 0x44, 0x46]),
                new EmailAttachment("Invoice-TB100001.pdf", "application/pdf", [0x25, 0x50, 0x44, 0x46])
            ]);

        var mime = SmtpEmailSender.Build(message, Settings);

        Assert.Equal("\"Ghuri\" <bookings@ghuri.local>", mime.From.ToString());
        Assert.Equal(("rahim@example.com", "Booking confirmed - TB100001"), (mime.To.Mailboxes.Single().Address, mime.Subject));
        Assert.Equal("<p>Hi Rahim</p>", mime.HtmlBody);
        Assert.Equal(new[] { "Voucher-TB100001.pdf", "Invoice-TB100001.pdf" },
            mime.Attachments.OfType<MimePart>().Select(a => a.FileName));
        Assert.All(mime.Attachments.OfType<MimePart>(), a => Assert.Equal("application/pdf", a.ContentType.MimeType));
    }

    [Fact]
    public void AnEmailWithoutAttachments_IsJustTheBody()
    {
        var mime = SmtpEmailSender.Build(new EmailMessage("rahim@example.com", "Reset your password", "<p>Link</p>"), Settings);

        Assert.Empty(mime.Attachments);
        Assert.Equal("<p>Link</p>", mime.HtmlBody);
    }
}
