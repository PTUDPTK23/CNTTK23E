using System.Text.Json;
using Notification.Shared;
using RabbitMQ.Client;

namespace Campaign.Api;

public sealed class OutboxPublisher(NotificationStore store, Broker broker, ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await broker.ConnectAsync("campaign-outbox", stoppingToken);
                var options = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
                await using var channel = await connection.CreateChannelAsync(options, stoppingToken);
                await Broker.DeclareAsync(channel, stoppingToken);
                await Broker.DeclareAuditAsync(channel, stoppingToken);
                while (connection.IsOpen && channel.IsOpen && !stoppingToken.IsCancellationRequested)
                {
                    var published = await store.PublishNextAsync(async job =>
                    {
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        timeout.CancelAfter(TimeSpan.FromSeconds(10));
                        await channel.BasicPublishAsync(Broker.Exchange, Broker.RoutingKey, mandatory: true,
                            basicProperties: new BasicProperties
                            {
                                Persistent = true, ContentType = "application/json", MessageId = job.MessageId.ToString(),
                                CorrelationId = job.CampaignId.ToString(), Type = "SendNotification"
                            }, body: JsonSerializer.SerializeToUtf8Bytes(job), cancellationToken: timeout.Token);
                        logger.LogInformation("Published CampaignId={CampaignId} DeliveryId={DeliveryId} Generation={Generation}",
                            job.CampaignId, job.DeliveryId, job.Generation);
                    }, stoppingToken);
                    var audited = await store.PublishNextAuditAsync(async item =>
                    {
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        timeout.CancelAfter(TimeSpan.FromSeconds(10));
                        await channel.BasicPublishAsync(Broker.AuditExchange, $"delivery.{item.Outcome.ToLowerInvariant()}",
                            mandatory: true, basicProperties: new BasicProperties
                            {
                                Persistent = true, ContentType = "application/json", MessageId = item.EventId.ToString(),
                                CorrelationId = item.CampaignId.ToString(), Type = "DeliveryAuditEvent"
                            }, body: JsonSerializer.SerializeToUtf8Bytes(item), cancellationToken: timeout.Token);
                    }, stoppingToken);
                    if (!published && !audited) await Task.Delay(500, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning("Outbox tạm dừng: {ErrorType}; thử kết nối lại sau 3 giây", ex.GetType().Name);
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
