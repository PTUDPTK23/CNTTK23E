using System.Text;
using System.Text.Json;
using Auction.Hub.Hubs;
using AuctionSystem.Shared.Common;
using AuctionSystem.Shared.Events;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Auction.Hub.Consumers;

public class RabbitMqHubConsumer : BackgroundService
{
    private readonly IHubContext<AuctionHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqHubConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqHubConsumer(
        IHubContext<AuctionHub> hubContext,
        IConfiguration configuration,
        ILogger<RabbitMqHubConsumer> logger)
    {
        _hubContext = hubContext;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _configuration.GetValue<string>("RabbitMQ:Host", "localhost")!;
        var port = _configuration.GetValue<int>("RabbitMQ:Port", 5672);
        var user = _configuration.GetValue<string>("RabbitMQ:Username", "guest")!;
        var pass = _configuration.GetValue<string>("RabbitMQ:Password", "guest")!;

        _logger.LogInformation("Auction.Hub consumer waiting to connect to RabbitMQ at {Host}:{Port}...", host, port);

        // Retry connection loop until connected or cancelled
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

                // Declare queue for SignalR Hub
                _channel.QueueDeclare(
                    queue: RabbitMqConstants.HubQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // Bind queue to exchange with wildcard
                _channel.QueueBind(
                    queue: RabbitMqConstants.HubQueueName,
                    exchange: RabbitMqConstants.ExchangeName,
                    routingKey: RabbitMqConstants.RoutingKeyAllAuctionEvents);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var routingKey = ea.RoutingKey;
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var correlationId = ea.BasicProperties?.CorrelationId ?? string.Empty;

                    _logger.LogInformation("Hub received RabbitMQ event [{RoutingKey}], CorrelationId: {CorrelationId}",
                        routingKey, correlationId);

                    try
                    {
                        if (routingKey == RabbitMqConstants.RoutingKeyBidAccepted)
                        {
                            var bidEvent = JsonSerializer.Deserialize<BidAcceptedEvent>(json);
                            if (bidEvent != null)
                            {
                                var groupName = $"auction-{bidEvent.AuctionId}";
                                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveBidUpdate", bidEvent, cancellationToken: stoppingToken);
                                _logger.LogInformation("Broadcasted ReceiveBidUpdate to group {GroupName}: Amount={Amount:N0}, Bidder={Bidder}",
                                    groupName, bidEvent.Amount, bidEvent.Bidder);
                            }
                        }
                        else if (routingKey == RabbitMqConstants.RoutingKeyAuctionEnded)
                        {
                            var endedEvent = JsonSerializer.Deserialize<AuctionEndedEvent>(json);
                            if (endedEvent != null)
                            {
                                var groupName = $"auction-{endedEvent.AuctionId}";
                                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveAuctionEnded", endedEvent, cancellationToken: stoppingToken);
                                _logger.LogInformation("Broadcasted ReceiveAuctionEnded to group {GroupName}: Winner={Winner}",
                                    groupName, endedEvent.Winner);
                            }
                        }
                        else if (routingKey == RabbitMqConstants.RoutingKeyAuctionCreated)
                        {
                            var createdEvent = JsonSerializer.Deserialize<AuctionCreatedEvent>(json);
                            if (createdEvent != null)
                            {
                                await _hubContext.Clients.All.SendAsync("ReceiveNewAuction", createdEvent, cancellationToken: stoppingToken);
                                _logger.LogInformation("Broadcasted ReceiveNewAuction to All clients: {Title}", createdEvent.Title);
                            }
                        }

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event from RabbitMQ: {RoutingKey}", routingKey);
                        // Requeue or nack
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                _channel.BasicConsume(
                    queue: RabbitMqConstants.HubQueueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("Auction.Hub consumer started listening on queue {QueueName}", RabbitMqConstants.HubQueueName);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ for Auction.Hub. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        // Keep service alive
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
