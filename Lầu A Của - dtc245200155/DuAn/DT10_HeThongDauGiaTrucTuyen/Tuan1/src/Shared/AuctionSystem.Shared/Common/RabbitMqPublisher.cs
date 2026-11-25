using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AuctionSystem.Shared.Common;

public interface IRabbitMqPublisher
{
    void Publish<T>(string routingKey, T message, string? correlationId = null);
}

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly string _hostName;
    private readonly int _port;
    private readonly string _userName;
    private readonly string _password;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new();
    private bool _disposed;

    public RabbitMqPublisher(
        string hostName,
        int port,
        string userName,
        string password,
        ILogger<RabbitMqPublisher> logger)
    {
        _hostName = hostName;
        _port = port;
        _userName = userName;
        _password = password;
        _logger = logger;
    }

    private void EnsureConnected()
    {
        if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
            return;

        lock (_lock)
        {
            if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
                return;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _hostName,
                    Port = _port,
                    UserName = _userName,
                    Password = _password,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(2),
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                _connection?.Dispose();
                _channel?.Dispose();

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare main topic exchange
                _channel.ExchangeDeclare(
                    exchange: RabbitMqConstants.ExchangeName,
                    type: RabbitMqConstants.ExchangeType,
                    durable: true,
                    autoDelete: false);

                _logger.LogInformation("Successfully connected to RabbitMQ at {Host}:{Port}", _hostName, _port);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ connection failed at {Host}:{Port}: {Message}", _hostName, _port, ex.Message);
                throw;
            }
        }
    }

    public void Publish<T>(string routingKey, T message, string? correlationId = null)
    {
        try
        {
            EnsureConnected();

            lock (_lock)
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.CorrelationId = correlationId ?? Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange: RabbitMqConstants.ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Published event to RabbitMQ [{RoutingKey}] CorrelationId: {CorrelationId}",
                    routingKey, properties.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ offline. Event [{RoutingKey}] could not be published: {Message}", routingKey, ex.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch { }
    }
}
