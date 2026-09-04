using RegistrarMain.Contracts;

namespace RegistrarMain.Services;

public interface INotificationService
{
    Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
