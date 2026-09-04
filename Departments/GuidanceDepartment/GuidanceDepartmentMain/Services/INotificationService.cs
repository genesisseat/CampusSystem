using GuidanceDepartmentMain.Contracts;

namespace GuidanceDepartmentMain.Services;

public interface INotificationService
{
    Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}