using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Hubs;
using NotificationService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Services;

public class RabbitMqConsumerWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<RabbitMqConsumerWorker> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumerWorker(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationHub> hubContext,
        ILogger<RabbitMqConsumerWorker> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ConnectAndConsume();
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ connection failed ({Error}). Retrying in 3 seconds...", ex.Message);
                Cleanup();
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private void ConnectAndConsume()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true
        };

        _connection = factory.CreateConnection("notification-service-consumer");
        _channel = _connection.CreateModel();

        var exchange = _configuration["RabbitMQ:Exchange"] ?? "document.events";
        var queue = _configuration["RabbitMQ:Queue"] ?? "notification.queue";
        var dlxExchange = $"{exchange}.dlx";
        var dlqQueue = "notification.dlq";

        // Declare Dead-Letter Exchange & Queue
        _channel.ExchangeDeclare(dlxExchange, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(dlqQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(dlqQueue, dlxExchange, "notification.dead");

        // Declare Main Exchange & Queue with DLX binding
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", dlxExchange },
            { "x-dead-letter-routing-key", "notification.dead" }
        };
        _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind(queue, exchange, "document.*");
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, ea) =>
        {
            HandleMessageAsync(ea).GetAwaiter().GetResult();
        };

        _channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
        _logger.LogInformation("NotificationService listening on RabbitMQ queue '{Queue}' (exchange '{Exchange}').", queue, exchange);
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
    {
        var bodyText = Encoding.UTF8.GetString(ea.Body.ToArray());
        var correlationId = ea.BasicProperties?.CorrelationId ?? "unknown";

        try
        {
            var evt = JsonSerializer.Deserialize<DocumentStatusChangedEvent>(bodyText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (evt is null)
            {
                _logger.LogError("[CorrelationId={CorrelationId}] Failed to deserialize event payload. Sending to DLQ.", correlationId);
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            // Idempotent Consumer check
            var alreadyProcessed = await db.ProcessedEvents.AnyAsync(p => p.EventId == evt.EventId);
            if (alreadyProcessed)
            {
                _logger.LogInformation("[CorrelationId={CorrelationId}] Duplicate EventId {EventId} ignored (Idempotent Consumer).",
                    evt.CorrelationId, evt.EventId);
                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            var notifications = BuildNotifications(evt);
            db.Notifications.AddRange(notifications);
            db.ProcessedEvents.Add(new ProcessedEvent
            {
                EventId = evt.EventId,
                EventType = evt.EventType,
                ProcessedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            // Push real-time notifications to connected clients via SignalR
            foreach (var item in notifications)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", item);
            }

            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
            _logger.LogInformation(
                "[CorrelationId={CorrelationId}] Processed {EventType} for Document {DocumentId}, sent {Count} real-time notifications.",
                evt.CorrelationId, evt.EventType, evt.DocumentId, notifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CorrelationId={CorrelationId}] Error processing message. Routing to DLQ.", correlationId);
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private static List<NotificationItem> BuildNotifications(DocumentStatusChangedEvent evt)
    {
        var now = DateTime.UtcNow;
        var list = new List<NotificationItem>();
        var commentSuffix = string.IsNullOrWhiteSpace(evt.Comment) ? "" : $" (Ghi chú: {evt.Comment})";

        switch (evt.EventType)
        {
            case "DocumentSubmitted":
                list.Add(new NotificationItem
                {
                    Id = Guid.NewGuid(),
                    EventId = evt.EventId,
                    DocumentId = evt.DocumentId,
                    DocumentTitle = evt.Title,
                    EventType = evt.EventType,
                    Recipient = evt.Approver,
                    RecipientRole = "Approver",
                    Title = "Tài liệu mới chờ phê duyệt",
                    Message = $"Người tạo '{evt.CreatedBy}' đã gửi tài liệu '{evt.Title}' chờ bạn phê duyệt.{commentSuffix}",
                    CorrelationId = evt.CorrelationId,
                    CreatedAt = now
                });
                list.Add(new NotificationItem
                {
                    Id = Guid.NewGuid(),
                    EventId = evt.EventId,
                    DocumentId = evt.DocumentId,
                    DocumentTitle = evt.Title,
                    EventType = evt.EventType,
                    Recipient = evt.CreatedBy,
                    RecipientRole = "Creator",
                    Title = "Gửi phê duyệt thành công",
                    Message = $"Tài liệu '{evt.Title}' đã được chuyển tới người phê duyệt '{evt.Approver}'.",
                    CorrelationId = evt.CorrelationId,
                    CreatedAt = now
                });
                break;

            case "DocumentApproved":
                list.Add(new NotificationItem
                {
                    Id = Guid.NewGuid(),
                    EventId = evt.EventId,
                    DocumentId = evt.DocumentId,
                    DocumentTitle = evt.Title,
                    EventType = evt.EventType,
                    Recipient = evt.CreatedBy,
                    RecipientRole = "Creator",
                    Title = "Tài liệu đã được phê duyệt",
                    Message = $"Tài liệu '{evt.Title}' của bạn đã được '{evt.Actor}' phê duyệt.{commentSuffix}",
                    CorrelationId = evt.CorrelationId,
                    CreatedAt = now
                });
                break;

            case "DocumentRejected":
                list.Add(new NotificationItem
                {
                    Id = Guid.NewGuid(),
                    EventId = evt.EventId,
                    DocumentId = evt.DocumentId,
                    DocumentTitle = evt.Title,
                    EventType = evt.EventType,
                    Recipient = evt.CreatedBy,
                    RecipientRole = "Creator",
                    Title = "Tài liệu bị từ chối",
                    Message = $"Tài liệu '{evt.Title}' của bạn đã bị '{evt.Actor}' từ chối.{commentSuffix}",
                    CorrelationId = evt.CorrelationId,
                    CreatedAt = now
                });
                break;
        }

        return list;
    }

    private void Cleanup()
    {
        try { _channel?.Close(); _channel?.Dispose(); } catch { }
        try { _connection?.Close(); _connection?.Dispose(); } catch { }
        _channel = null;
        _connection = null;
    }

    public override void Dispose()
    {
        Cleanup();
        base.Dispose();
    }
}
