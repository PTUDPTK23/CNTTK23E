using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared;

namespace DocumentApi.Services;

public interface IEventPublisher
{
    Task<bool> PublishDocumentStatusChangedAsync(DocumentStatusChangedEvent evt);
}

public class RabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly object _syncLock = new();
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private IModel GetOrCreateChannel()
    {
        if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
        {
            return _channel;
        }

        lock (_syncLock)
        {
            if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
            {
                return _channel;
            }

            _channel?.Dispose();
            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            _connection = factory.CreateConnection("document-api-publisher");
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: RabbitMqTopology.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            return _channel;
        }
    }

    public async Task<bool> PublishDocumentStatusChangedAsync(DocumentStatusChangedEvent evt)
    {
        const int maxRetries = 3;
        var routingKey = $"document.{evt.NewStatus.ToLowerInvariant()}";
        var payload = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(payload);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var channel = GetOrCreateChannel();
                var props = channel.CreateBasicProperties();
                props.Persistent = true;
                props.ContentType = "application/json";
                props.MessageId = evt.EventId.ToString();
                props.CorrelationId = evt.CorrelationId;
                props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                lock (_syncLock)
                {
                    channel.BasicPublish(
                        exchange: RabbitMqTopology.ExchangeName,
                        routingKey: routingKey,
                        basicProperties: props,
                        body: body
                    );
                }

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] Đã phát sự kiện {RoutingKey} cho tài liệu {DocumentId} ({OldStatus} -> {NewStatus})",
                    evt.CorrelationId, routingKey, evt.DocumentId, evt.OldStatus, evt.NewStatus);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[CorrelationId={CorrelationId}] Lỗi gửi RabbitMQ lần {Attempt}/{MaxRetries} cho tài liệu {DocumentId}",
                    evt.CorrelationId, attempt, maxRetries, evt.DocumentId);

                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
                }
            }
        }

        _logger.LogError(
            "[CorrelationId={CorrelationId}] Không thể gửi sự kiện tới RabbitMQ sau {MaxRetries} lần thử. Dữ liệu đã được lưu an toàn trong Database.",
            evt.CorrelationId, maxRetries);
        return false;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
