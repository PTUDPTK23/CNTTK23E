using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Contracts.DTOs;

namespace CampaignApi.Services;

public interface IRabbitMqPublisher
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    Task PublishNotificationAsync(NotificationJobMessage message, CancellationToken cancellationToken = default);
    Task<int> PublishBatchAsync(
        IEnumerable<NotificationJobMessage> messages,
        Func<Guid, Task> onMessagePublishedAsync,
        CancellationToken cancellationToken = default);
}

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly ConnectionFactory _factory;
    private readonly string _queueName;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var host = _configuration["RabbitMQ:HostName"] 
            ?? Environment.GetEnvironmentVariable("RabbitMQ__HostName") 
            ?? "localhost";

        var portStr = _configuration["RabbitMQ:Port"] 
            ?? Environment.GetEnvironmentVariable("RabbitMQ__Port") 
            ?? "5672";
        int.TryParse(portStr, out var port);
        if (port <= 0) port = 5672;

        var user = _configuration["RabbitMQ:UserName"] 
            ?? Environment.GetEnvironmentVariable("RabbitMQ__UserName") 
            ?? "guest";

        var pass = _configuration["RabbitMQ:Password"] 
            ?? Environment.GetEnvironmentVariable("RabbitMQ__Password") 
            ?? "guest";

        _queueName = _configuration["RabbitMQ:QueueName"] 
            ?? Environment.GetEnvironmentVariable("RabbitMQ__QueueName") 
            ?? "notification_queue";

        _factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(3),
            AutomaticRecoveryEnabled = true
        };
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _factory.CreateConnectionAsync(cancellationToken);
            return connection.IsOpen;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể kết nối tới RabbitMQ broker ({Host}:{Port})", _factory.HostName, _factory.Port);
            return false;
        }
    }

    public async Task PublishNotificationAsync(NotificationJobMessage message, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = message.JobId.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _queueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("✅ Đã publish job [{JobId}] vào queue [{QueueName}] cho người nhận [{RecipientName}]",
            message.JobId, _queueName, message.RecipientName);
    }

    public async Task<int> PublishBatchAsync(
        IEnumerable<NotificationJobMessage> messages,
        Func<Guid, Task> onMessagePublishedAsync,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Khai báo hàng đợi bền vững (durable queue)
        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        int count = 0;
        foreach (var message in messages)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            // Cấu hình persistent message (DeliveryMode = Persistent)
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = message.JobId.ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queueName,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            count++;
            await onMessagePublishedAsync(message.JobId);

            _logger.LogInformation("📨 [Batch] Đã đưa job [{JobId}] vào queue [{QueueName}] ({Count})",
                message.JobId, _queueName, count);
        }

        return count;
    }
}
