using FinanceMain.Contracts;

namespace FinanceMain.Services;

public interface INotificationService
{
    Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
