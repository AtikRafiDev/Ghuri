namespace Ghuri.Domain.Enums;

/// <summary>Maps to notify.Notifications.Status (TINYINT).</summary>
public enum NotificationStatus : byte
{
    Queued = 1,
    Sent = 2,
    Failed = 3
}
