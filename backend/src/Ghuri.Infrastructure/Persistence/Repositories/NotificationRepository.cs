using Ghuri.Domain.Entities.Notify;
using Ghuri.Domain.Repositories;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of INotificationRepository.</summary>
internal sealed class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public void Add(Notification notification) => db.Notifications.Add(notification);
}
