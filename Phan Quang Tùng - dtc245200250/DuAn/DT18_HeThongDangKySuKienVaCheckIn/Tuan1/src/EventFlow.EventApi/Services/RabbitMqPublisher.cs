using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EventFlow.EventApi.Services;

public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<IChannel?> GetChannelAsync()
    {
        if (_channel != null && _channel.IsOpen)
        {
            return _channel;
        }

        await _lock.WaitAsync();
        try
        {
            if (_channel != null && _channel.IsOpen)
            {
                return _channel;
            }

            var host = _configuration["RabbitMQ:Host"] ?? "localhost";
            var port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672;
            var user = _configuration["RabbitMQ:Username"] ?? "guest";
            var pass = _configuration["RabbitMQ:Password"] ?? "guest";

            _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port} as {User}...", host, port, user);

            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = user,
                Password = pass,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(
                exchange: RabbitMqConstants.ExchangeName,
                type: RabbitMqConstants.ExchangeType,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("Successfully connected to RabbitMQ and declared exchange '{Exchange}'", RabbitMqConstants.ExchangeName);
            return _channel;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not establish connection to RabbitMQ ({Host}). Publishing might be skipped or queued.", _configuration["RabbitMQ:Host"] ?? "localhost");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PublishAsync<T>(string routingKey, T message) where T : class, IEventMessage
    {
        try
        {
            var channel = await GetChannelAsync();
            if (channel == null)
            {
                _logger.LogWarning("RabbitMQ channel not available. Skipping publish for event {EventType} (Correlation: {CorrelationId})", message.EventType, message.CorrelationId);
                return;
            }

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                MessageId = message.MessageId,
                CorrelationId = message.CorrelationId
            };

            var body = JsonSerializer.SerializeToUtf8Bytes(message);

            await channel.BasicPublishAsync(
                exchange: RabbitMqConstants.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);

            _logger.LogInformation("Published {EventType} to exchange '{Exchange}' with routing key '{RoutingKey}' (Correlation: {CorrelationId})",
                message.EventType, RabbitMqConstants.ExchangeName, routingKey, message.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {EventType} with routing key {RoutingKey}", message.EventType, routingKey);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel != null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
        }
        catch
        {
            // Ignore during cleanup
        }
    }
}
