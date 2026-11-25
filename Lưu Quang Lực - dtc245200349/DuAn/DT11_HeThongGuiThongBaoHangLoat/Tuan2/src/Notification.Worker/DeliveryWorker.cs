using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Shared;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.Worker;

public sealed class DeliveryWorker(NotificationStore store, Broker broker, EmailSender sender,
    IConfiguration config, ILogger<DeliveryWorker> logger) : BackgroundService
{
    private readonly string workerId = $"{Environment.MachineName}-{Guid.NewGuid().ToString("N")[..8]}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maximum = Math.Clamp(config.GetValue("Delivery:MaxAttempts", 3), 1, 10);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await broker.ConnectAsync(workerId, stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await Broker.DeclareAsync(channel, stoppingToken);
                await channel.BasicQosAsync(0, 1, false, stoppingToken);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, message) =>
                {
                    SendNotification? job;
                    try
                    {
                        job = JsonSerializer.Deserialize<SendNotification>(message.Body.Span);
                        if (job is null || job.SchemaVersion != 1 || job.Generation < 1 ||
                            job.MessageId == Guid.Empty || job.CampaignId == Guid.Empty || job.DeliveryId == Guid.Empty)
                            throw new JsonException("Invalid job envelope.");
                    }
                    catch (JsonException)
                    {
                        logger.LogWarning("Invalid message -> dead-letter queue WorkerId={WorkerId}", workerId);
                        await channel.BasicRejectAsync(message.DeliveryTag, requeue: false, cancellationToken: stoppingToken);
                        return;
                    }
                    try
                    {
                        var delivery = await store.ClaimAsync(job, workerId, stoppingToken);
                        if (delivery is not null)
                        {
                            logger.LogInformation("Sending CampaignId={CampaignId} DeliveryId={DeliveryId} WorkerId={WorkerId} Attempt={Attempt}",
                                delivery.CampaignId, delivery.Id, workerId, delivery.AttemptCount);
                            var result = await sender.SendAsync(delivery, stoppingToken);
                            await store.CompleteAsync(delivery, result.Error, result.Permanent, maximum, stoppingToken);
                            logger.LogInformation("Processed DeliveryId={DeliveryId} WorkerId={WorkerId} Success={Success}",
                                delivery.Id, workerId, result.Error is null);
                        }
                        // Message trùng, phiên cũ hoặc lease đang hoạt động: DB/scheduler giữ quyền phục hồi.
                        await channel.BasicAckAsync(message.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Processing interrupted DeliveryId={DeliveryId} ErrorType={ErrorType}", job.DeliveryId, ex.GetType().Name);
                        await Task.Delay(3000, stoppingToken);
                        if (channel.IsOpen)
                            await channel.BasicNackAsync(message.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                    }
                };
                await channel.BasicConsumeAsync(Broker.Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
                logger.LogInformation("Worker ready WorkerId={WorkerId}", workerId);
                while (connection.IsOpen && channel.IsOpen && !stoppingToken.IsCancellationRequested)
                    await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning("Worker reconnect WorkerId={WorkerId} ErrorType={ErrorType}", workerId, ex.GetType().Name);
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
