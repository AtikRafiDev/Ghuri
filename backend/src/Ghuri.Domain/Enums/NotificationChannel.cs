namespace Ghuri.Domain.Enums;

/// <summary>Maps to notify.NotificationTemplates.Channel and notify.Notifications.Channel (TINYINT).</summary>
public enum NotificationChannel : byte
{
    Email = 1,
    Sms = 2,
    InApp = 3
}
