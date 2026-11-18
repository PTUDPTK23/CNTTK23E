using DocumentApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DocumentApi.Services;

public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        RabbitMqPublisher publisher,
        ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();

                var pendingMessages = await db.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in pendingMessages)
                {
                    try
                    {
                        _publisher.Publish(msg.Id, msg.RoutingKey, msg.Payload, msg.CorrelationId);
                        msg.ProcessedAt = DateTime.UtcNow;
                        msg.LastError = null;
                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "[CorrelationId={CorrelationId}] Published event {EventType} (Id={EventId}) to RabbitMQ.",
                            msg.CorrelationId, msg.EventType, msg.Id);
                    }
                    catch (Exception ex)
                    {
                        msg.RetryCount++;
                        msg.LastError = ex.Message;
                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogWarning(
                            "[CorrelationId={CorrelationId}] RabbitMQ unavailable for OutboxMessage {EventId} (Retry #{Retry}): {Error}",
                            msg.CorrelationId, msg.Id, msg.RetryCount, ex.Message);
                        break;
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error in OutboxPublisherWorker loop.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
