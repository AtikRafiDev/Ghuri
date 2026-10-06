using Ghuri.Domain.Entities.Notify;

namespace Ghuri.Domain.Repositories;

/// <summary>
/// Queues messages in notify.Notifications - e.g. the SMS "your quote is
/// ready" (Day 13: "SMS-ready template"). Sending SMS is Phase 2: until
/// then the rows wait as Queued, written and ready.
/// </summary>
/// <remarks>
/// Used by outbox handlers: the outbox job saves the row together with
/// marking the event done (same scope, same DbContext).
/// </remarks>
public interface INotificationRepository
{
    void Add(Notification notification);
}
