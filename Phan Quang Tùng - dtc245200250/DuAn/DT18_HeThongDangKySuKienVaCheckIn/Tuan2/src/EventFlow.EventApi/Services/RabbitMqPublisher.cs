using System;
using System.Net.Http;
using System.Net.Http.Json;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(
        IConfiguration configuration, 
        ILogger<RabbitMqPublisher> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
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
            _logger.LogWarning("RabbitMQ not reachable at {Host}:{Port} ({Msg}). Event will also be notified directly to DashboardService.", 
                _configuration["RabbitMQ:Host"] ?? "localhost", _configuration["RabbitMQ:Port"] ?? "5672", ex.Message);
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PublishAsync<T>(string routingKey, T message) where T : class, IEventMessage
    {
        // 1. Thử gửi qua RabbitMQ nếu có broker hoạt động
        try
        {
            var channel = await GetChannelAsync();
            if (channel != null)
            {
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

                _logger.LogInformation("Published {EventType} to exchange '{Exchange}' with routing key '{RoutingKey}'",
                    message.EventType, RabbitMqConstants.ExchangeName, routingKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ publish error: {Msg}", ex.Message);
        }

        // 2. Đồng thời gửi thông báo trực tiếp tới DashboardService để phát sóng SignalR tức thì
        try
        {
            var dashboardUrl = _configuration["Services:DashboardServiceUrl"] ?? "http://localhost:5024";
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(dashboardUrl);
            client.Timeout = TimeSpan.FromSeconds(3);

            string endpoint = routingKey switch
            {
                RabbitMqConstants.RoutingKeyRegistered => "/api/dashboard/notify/registration",
                RabbitMqConstants.RoutingKeyCheckedIn => "/api/dashboard/notify/checkin",
                RabbitMqConstants.RoutingKeyCancelled => "/api/dashboard/notify/cancel",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(endpoint))
            {
                var response = await client.PostAsJsonAsync(endpoint, message);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Directly notified DashboardService for SignalR broadcast ({Endpoint})", endpoint);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Direct Dashboard notification skipped or offline: {Msg}", ex.Message);
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
