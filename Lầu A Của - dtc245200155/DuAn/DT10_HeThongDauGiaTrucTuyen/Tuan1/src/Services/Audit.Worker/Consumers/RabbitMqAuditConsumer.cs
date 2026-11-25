using System.Text;
using AuctionSystem.Shared.Common;
using AuctionSystem.Shared.Models;
using Audit.Worker.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Audit.Worker.Consumers;

public class RabbitMqAuditConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqAuditConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqAuditConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<RabbitMqAuditConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _configuration.GetValue<string>("RabbitMQ:Host", "localhost")!;
        var port = _configuration.GetValue<int>("RabbitMQ:Port", 5672);
        var user = _configuration.GetValue<string>("RabbitMQ:Username", "guest")!;
        var pass = _configuration.GetValue<string>("RabbitMQ:Password", "guest")!;

        _logger.LogInformation("Audit.Worker waiting to connect to RabbitMQ at {Host}:{Port}...", host, port);

        while (!stoppingToken.IsCancellationRequested && (_channel == null || !_channel.IsOpen))
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = host,
                    Port = port,
                    UserName = user,
                    Password = pass,
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare topic exchange
                _channel.ExchangeDeclare(
                    exchange: RabbitMqConstants.ExchangeName,
                    type: RabbitMqConstants.ExchangeType,
                    durable: true,
                    autoDelete: false);

                // Declare queue for Audit Worker
                _channel.QueueDeclare(
                    queue: RabbitMqConstants.AuditQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // Bind queue to listen to all events
                _channel.QueueBind(
                    queue: RabbitMqConstants.AuditQueueName,
                    exchange: RabbitMqConstants.ExchangeName,
                    routingKey: RabbitMqConstants.RoutingKeyAllAuctionEvents);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var routingKey = ea.RoutingKey;
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var correlationId = ea.BasicProperties?.CorrelationId ?? string.Empty;

                    _logger.LogInformation("Audit received event [{RoutingKey}] CorrelationId: {CorrelationId}",
                        routingKey, correlationId);

                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

                        // Parse auctionId from json safely
                        string auctionId = "Unknown";
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("AuctionId", out var prop))
                            {
                                auctionId = prop.GetString() ?? prop.GetRawText();
                            }
                        }
                        catch { }

                        var auditLog = new AuditEntry
                        {
                            Id = Guid.NewGuid(),
                            AuctionId = auctionId,
                            EventType = routingKey,
                            Payload = json,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        };

                        dbContext.AuditEntries.Add(auditLog);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Persisted AuditEntry [{Id}] for Auction {AuctionId}, Event: {EventType}",
                            auditLog.Id, auctionId, routingKey);

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist audit log for event {RoutingKey}", routingKey);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                _channel.BasicConsume(
                    queue: RabbitMqConstants.AuditQueueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("Audit.Worker started consuming from queue {QueueName}", RabbitMqConstants.AuditQueueName);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ for Audit.Worker. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
