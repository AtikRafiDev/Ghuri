using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Notify;

/// <summary>A bilingual message template, e.g. BOOKING_CONFIRMED in English by email (blueprint: notify.NotificationTemplates [A]).</summary>
public sealed class NotificationTemplate : AggregateRoot, IAuditable
{
    public string Code { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }

    /// <summary>ISO 639-1 language code: "en" or "bn".</summary>
    public string Language { get; private set; } = string.Empty;

    public string? Subject { get; private set; }

    /// <summary>Contains placeholders like {{BookingNo}}, filled in when a notification is actually sent.</summary>
    public string Body { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    private NotificationTemplate()
    {
    }

    public static NotificationTemplate Create(string code, NotificationChannel channel, string language, string body, string? subject = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        return new NotificationTemplate
        {
            Code = code,
            Channel = channel,
            Language = language,
            Body = body,
            Subject = subject,
            IsActive = true
        };
    }
}
