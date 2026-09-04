using StudentPortalMain.Contracts;

namespace StudentPortalMain.Services;

public interface INotificationService
{
    Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
