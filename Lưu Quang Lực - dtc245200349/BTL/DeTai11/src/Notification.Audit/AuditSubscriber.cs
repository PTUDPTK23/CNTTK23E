using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Shared;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.Audit;

internal sealed class AuditSubscriber(NotificationStore store, Broker broker, ILogger<AuditSubscriber> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var connection = await broker.ConnectAsync("notification-audit", ct);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
                await Broker.DeclareAuditAsync(channel, ct);
                await channel.BasicQosAsync(0, 1, false, ct);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, message) => HandleAsync(channel, message, ct);
                await channel.BasicConsumeAsync(Broker.AuditQueue, autoAck: false, consumer: consumer, cancellationToken: ct);
                logger.LogInformation("Audit subscriber ready Queue={Queue}", Broker.AuditQueue);
                while (connection.IsOpen && channel.IsOpen && !ct.IsCancellationRequested)
                    await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning("Audit subscriber reconnect ErrorType={ErrorType}", ex.GetType().Name);
                await Task.Delay(3000, ct);
            }
        }
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs message, CancellationToken ct)
    {
        DeliveryAuditEvent? item;
        try
        {
            item = JsonSerializer.Deserialize<DeliveryAuditEvent>(message.Body.Span);
            if (item is null || !item.IsValid()) throw new JsonException("Invalid audit event");
        }
        catch (JsonException)
        {
            logger.LogWarning("Rejected malformed audit event");
            await channel.BasicRejectAsync(message.DeliveryTag, requeue: false, cancellationToken: ct);
            return;
        }

        try
        {
            await store.RecordAuditAsync(item, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Audit persistence failed EventId={EventId} ErrorType={ErrorType}", item.EventId, ex.GetType().Name);
            await Task.Delay(3000, ct);
            if (channel.IsOpen)
                await channel.BasicNackAsync(message.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
            return;
        }
        await channel.BasicAckAsync(message.DeliveryTag, multiple: false, cancellationToken: ct);
        logger.LogInformation("Audit recorded CampaignId={CampaignId} DeliveryId={DeliveryId} EventId={EventId} Outcome={Outcome}",
            item.CampaignId, item.DeliveryId, item.EventId, item.Outcome);
    }
}
