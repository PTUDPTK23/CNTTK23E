using System.Text;
using System.Text.Json;
using FileProcessing.Core.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace FileProcessing.Infrastructure.Messaging;

public interface IMessagePublisher
{
    Task<bool> PublishImageJobAsync(ProcessImageJobMessage message, CancellationToken cancellationToken = default);
}

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly IJobQueueChannel _fallbackQueue;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isDisposed;

    public RabbitMqPublisher(
        IOptions<RabbitMqOptions> options,
        IJobQueueChannel fallbackQueue,
        ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _fallbackQueue = fallbackQueue;
        _logger = logger;
    }

    private async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true }) return true;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true }) return true;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(2)
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Declare DLX and DLQ
            await _channel.ExchangeDeclareAsync(_options.DlxExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
            await _channel.QueueDeclareAsync(_options.DlqQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(_options.DlqQueueName, _options.DlxExchangeName, _options.RoutingKey, cancellationToken: cancellationToken);

            // Declare Main Exchange and Main Queue with DLX arguments
            await _channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", _options.DlxExchangeName },
                { "x-dead-letter-routing-key", _options.RoutingKey }
            };
            await _channel.QueueDeclareAsync(_options.MainQueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs, cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(_options.MainQueueName, _options.ExchangeName, _options.RoutingKey, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully connected to RabbitMQ broker and initialized topology.");
            return true;
        }
        catch (Exception ex) when (ex is BrokerUnreachableException or System.Net.Sockets.SocketException or TimeoutException)
        {
            _logger.LogWarning("RabbitMQ broker is not reachable ({Host}:{Port}). Operating in reliable fallback mode.", _options.HostName, _options.Port);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error connecting to RabbitMQ.");
            return false;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<bool> PublishImageJobAsync(ProcessImageJobMessage message, CancellationToken cancellationToken = default)
    {
        var connected = await EnsureConnectedAsync(cancellationToken);

        if (connected && _channel is { IsOpen: true })
        {
            try
            {
                var payloadJson = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(payloadJson);

                var props = new BasicProperties
                {
                    Persistent = true,
                    MessageId = message.MessageId.ToString(),
                    CorrelationId = message.JobId.ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    ContentType = "application/json"
                };

                await _channel.BasicPublishAsync(
                    exchange: _options.ExchangeName,
                    routingKey: _options.RoutingKey,
                    mandatory: true,
                    basicProperties: props,
                    body: body,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Published Job {JobId} message to RabbitMQ exchange {Exchange}.", message.JobId, _options.ExchangeName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish to RabbitMQ. Enqueuing to fallback channel.");
            }
        }

        // Fallback: enqueue into memory channel so processing continues uninterrupted
        await _fallbackQueue.EnqueueAsync(message, cancellationToken);
        _logger.LogInformation("Job {JobId} dispatched to local queue channel.", message.JobId);
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _initLock.Dispose();
    }
}
