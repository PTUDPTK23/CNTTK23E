using Notification.Shared;

namespace Campaign.Api;

public sealed class RetryScheduler(NotificationStore store, IConfiguration config, ILogger<RetryScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maximum = Math.Clamp(config.GetValue("Delivery:MaxAttempts", 3), 1, 10);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Giới hạn công việc mỗi vòng để không giữ vòng lặp vô hạn khi tồn đọng lớn.
                for (var i = 0; i < 100 && await store.RecoverNextAsync(maximum, stoppingToken); i++) { }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Scheduler tạm dừng: {ErrorType}", ex.GetType().Name); }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
