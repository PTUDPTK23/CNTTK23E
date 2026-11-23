using System.Text;
using RabbitMQ.Client;

namespace DocumentApi.Services;

public class RabbitMqPublisher : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly object _lock = new();
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private IModel GetOrCreateChannel()
    {
        lock (_lock)
        {
            if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
            {
                return _channel;
            }

            CleanupUnsafe();

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            };

            _connection = factory.CreateConnection("document-api-publisher");
            _channel = _connection.CreateModel();

            var exchange = _configuration["RabbitMQ:Exchange"] ?? "document.events";
            _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);

            _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}, exchange '{Exchange}' ready.",
                factory.HostName, factory.Port, exchange);

            return _channel;
        }
    }

    public void Publish(Guid messageId, string routingKey, string payloadJson, string correlationId)
    {
        var channel = GetOrCreateChannel();
        var exchange = _configuration["RabbitMQ:Exchange"] ?? "document.events";
        var body = Encoding.UTF8.GetBytes(payloadJson);

        lock (_lock)
        {
            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.MessageId = messageId.ToString();
            props.CorrelationId = correlationId;
            props.ContentType = "application/json";
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: props,
                body: body);
        }
    }

    private void CleanupUnsafe()
    {
        try { _channel?.Close(); _channel?.Dispose(); } catch { }
        try { _connection?.Close(); _connection?.Dispose(); } catch { }
        _channel = null;
        _connection = null;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            CleanupUnsafe();
        }
    }
}
