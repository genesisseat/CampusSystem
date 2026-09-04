using LibraryMain.Contracts;

namespace LibraryMain.Services;

public interface INotificationService
{
    Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
