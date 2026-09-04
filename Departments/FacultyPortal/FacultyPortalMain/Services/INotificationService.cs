using FacultyPortalMain.Contracts;

namespace FacultyPortalMain.Services;

public interface INotificationService
{
    Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
