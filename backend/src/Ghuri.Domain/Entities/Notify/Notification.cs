using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Notify;

/// <summary>One queued or sent notification - outbound email/SMS queue and in-app inbox in one table. A "log" table (BIGINT IDENTITY).</summary>
public sealed class Notification
{
    public long Id { get; private set; }

    /// <summary>Null for notifications not tied to a specific user (rare, but the column allows it).</summary>
    public Guid? UserId { get; private set; }

    public NotificationChannel Channel { get; private set; }

    /// <summary>The phone number, email address, or user id string this actually goes to.</summary>
    public string Destination { get; private set; } = string.Empty;

    public string TemplateCode { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public byte Attempts { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public string? Error { get; private set; }

    /// <summary>In-app notifications only - ignored for Email/Sms.</summary>
    public bool IsRead { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private Notification()
    {
    }

    public static Notification Queue(
        NotificationChannel channel, string destination, string templateCode, string body, DateTime nowUtc,
        Guid? userId = null, string? subject = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        return new Notification
        {
            UserId = userId,
            Channel = channel,
            Destination = destination,
            TemplateCode = templateCode,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Queued,
            Attempts = 0,
            IsRead = false,
            CreatedAtUtc = nowUtc
        };
    }
}
