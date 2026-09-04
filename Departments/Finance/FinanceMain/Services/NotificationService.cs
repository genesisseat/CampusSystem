using FinanceMain.Contracts;
using Polly;

namespace FinanceMain.Services;

public sealed class NotificationService(IOutboundMessageTransport transport, IAuditLogService audit, ILogger<NotificationService> logger) : INotificationService
{
    private readonly ResiliencePipeline pipeline = new ResiliencePipelineBuilder().AddRetry(new Polly.Retry.RetryStrategyOptions { MaxRetryAttempts = 2 }).AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions { FailureRatio = 0.5, MinimumThroughput = 2, BreakDuration = TimeSpan.FromSeconds(30) }).Build();
    public async Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        try { await pipeline.ExecuteAsync(async token => await transport.SendAsync(message, token), cancellationToken); return true; }
        catch (Exception ex) { logger.LogError(ex, "Notification delivery failed"); await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), "system", "notification_failure", "Notification", message.Recipient, null, ex.GetType().Name, DateTimeOffset.UtcNow), cancellationToken); return false; }
    }
}
