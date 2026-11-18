using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;

namespace NotificationService.Services;

public class RabbitMqConsumerWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly NotificationStore _store;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<RabbitMqConsumerWorker> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumerWorker(
        IConfiguration configuration,
        NotificationStore store,
        IHubContext<NotificationHub> hubContext,
        ILogger<RabbitMqConsumerWorker> logger)
    {
        _configuration = configuration;
        _store = store;
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
                _logger.LogInformation("NotificationService đã kết nối RabbitMQ và đang lắng nghe sự kiện trên queue '{QueueName}'.", RabbitMqTopology.QueueName);

                while (!stoppingToken.IsCancellationRequested && _connection is { IsOpen: true } && _channel is { IsOpen: true })
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mất kết nối RabbitMQ. NotificationService sẽ tự động kết nối lại sau 5 giây...");
                Cleanup();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private void ConnectAndConsume()
    {
        Cleanup();

        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _connection = factory.CreateConnection("notification-service-consumer");
        _channel = _connection.CreateModel();

        // 1. Khai báo Dead-Letter Exchange & Dead-Letter Queue (DLQ)
        _channel.ExchangeDeclare(
            exchange: RabbitMqTopology.DeadLetterExchangeName,
            type: ExchangeType.Fanout,
            durable: true
        );

        _channel.QueueDeclare(
            queue: RabbitMqTopology.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        _channel.QueueBind(
            queue: RabbitMqTopology.DeadLetterQueueName,
            exchange: RabbitMqTopology.DeadLetterExchangeName,
            routingKey: string.Empty
        );

        // 2. Khai báo Main Exchange & Main Queue gắn với DLX
        _channel.ExchangeDeclare(
            exchange: RabbitMqTopology.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        );

        var queueArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = RabbitMqTopology.DeadLetterExchangeName
        };

        _channel.QueueDeclare(
            queue: RabbitMqTopology.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs
        );

        _channel.QueueBind(
            queue: RabbitMqTopology.QueueName,
            exchange: RabbitMqTopology.ExchangeName,
            routingKey: RabbitMqTopology.BindingRoutingKey
        );

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await HandleMessageAsync(ea);
        };

        _channel.BasicConsume(
            queue: RabbitMqTopology.QueueName,
            autoAck: false,
            consumer: consumer
        );
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        var rawJson = Encoding.UTF8.GetString(ea.Body.ToArray());

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var evt = JsonSerializer.Deserialize<DocumentStatusChangedEvent>(rawJson)
                    ?? throw new InvalidOperationException("Payload sự kiện không hợp lệ.");

                // Idempotent Consumer: bỏ qua nếu EventId đã được xử lý trước đó
                if (!_store.TryMarkProcessed(evt.EventId))
                {
                    _logger.LogInformation(
                        "[CorrelationId={CorrelationId}] Bỏ qua sự kiện trùng lặp EventId={EventId}",
                        evt.CorrelationId, evt.EventId);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                var (headline, detail) = BuildNotificationText(evt);

                var notification = new UserNotification(
                    Id: Guid.NewGuid(),
                    EventId: evt.EventId,
                    DocumentId: evt.DocumentId,
                    DocumentTitle: evt.Title,
                    Creator: evt.Creator,
                    Approver: evt.Approver,
                    NewStatus: evt.NewStatus,
                    ActionBy: evt.ActionBy,
                    Headline: headline,
                    DetailMessage: detail,
                    CorrelationId: evt.CorrelationId,
                    CreatedAtUtc: DateTime.UtcNow
                );

                _store.AddNotification(notification);

                // Đẩy thông báo real-time qua SignalR tới Client
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] Đã gửi thông báo tới [{Creator}] & [{Approver}]: {Headline}",
                    evt.CorrelationId, evt.Creator, evt.Approver, headline);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Lỗi xử lý message từ RabbitMQ (lần {Attempt}/3). DeliveryTag={DeliveryTag}",
                    attempt, ea.DeliveryTag);

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
                }
            }
        }

        // Sau 3 lần thử thất bại -> Nack không requeue để RabbitMQ đẩy vào Dead-Letter Queue
        _logger.LogError(
            "Chuyển message lỗi sang Dead-Letter Queue '{Dlq}'. Payload={Payload}",
            RabbitMqTopology.DeadLetterQueueName, rawJson);
        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
    }

    private static (string Headline, string Detail) BuildNotificationText(DocumentStatusChangedEvent evt)
    {
        return evt.NewStatus switch
        {
            DocumentStatuses.Submitted => (
                $"Hồ sơ mới chờ duyệt: '{evt.Title}'",
                $"Người tạo [{evt.Creator}] đã gửi tài liệu cho người phê duyệt [{evt.Approver}]. Ghi chú: {evt.Comment ?? "Không có"}"
            ),
            DocumentStatuses.Approved => (
                $"Tài liệu đã được PHÊ DUYỆT: '{evt.Title}'",
                $"Người phê duyệt [{evt.ActionBy}] đã chấp thuận tài liệu của [{evt.Creator}]. Ghi chú: {evt.Comment ?? "Đã duyệt"}"
            ),
            DocumentStatuses.Rejected => (
                $"Tài liệu bị TỪ CHỐI: '{evt.Title}'",
                $"Người phê duyệt [{evt.ActionBy}] đã từ chối tài liệu của [{evt.Creator}]. Lý do: {evt.Comment ?? "Cần bổ sung"}"
            ),
            _ => (
                $"Cập nhật trạng thái tài liệu: '{evt.Title}'",
                $"Trạng thái chuyển từ {evt.OldStatus} sang {evt.NewStatus} bởi [{evt.ActionBy}]."
            )
        };
    }

    private void Cleanup()
    {
        try { _channel?.Dispose(); } catch { }
        try { _connection?.Dispose(); } catch { }
        _channel = null;
        _connection = null;
    }

    public override void Dispose()
    {
        Cleanup();
        base.Dispose();
    }
}