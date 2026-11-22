using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventFlow.DashboardService.Services;

public class DashboardRabbitMqConsumer : BackgroundService
{
    private readonly IDashboardStateService _stateService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DashboardRabbitMqConsumer> _logger;

    public DashboardRabbitMqConsumer(
        IDashboardStateService stateService,
        IConfiguration configuration,
        ILogger<DashboardRabbitMqConsumer> logger)
    {
        _stateService = stateService;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DashboardRabbitMqConsumer started. Initializing baseline stats...");
        await _stateService.InitializeFromEventApiAsync();

        var host = _configuration["RabbitMQ:Host"] ?? "localhost";
        var port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672;
        var user = _configuration["RabbitMQ:Username"] ?? "guest";
        var pass = _configuration["RabbitMQ:Password"] ?? "guest";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("DashboardService connecting to RabbitMQ at {Host}:{Port}...", host, port);

                var factory = new ConnectionFactory
                {
                    HostName = host,
                    Port = port,
                    UserName = user,
                    Password = pass,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                using var connection = await factory.CreateConnectionAsync(stoppingToken);
                using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // Declare topic exchange
                await channel.ExchangeDeclareAsync(
                    exchange: RabbitMqConstants.ExchangeName,
                    type: RabbitMqConstants.ExchangeType,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                // Declare durable queue
                await channel.QueueDeclareAsync(
                    queue: RabbitMqConstants.DashboardQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                // Bind queue to topic exchange
                await channel.QueueBindAsync(
                    queue: RabbitMqConstants.DashboardQueue,
                    exchange: RabbitMqConstants.ExchangeName,
                    routingKey: RabbitMqConstants.RoutingKeyPatternAll,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("Dashboard queue bound to {Exchange} with routing key pattern '{Pattern}'",
                    RabbitMqConstants.ExchangeName, RabbitMqConstants.RoutingKeyPatternAll);

                // Set prefetch count
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var routingKey = ea.RoutingKey;

                        _logger.LogInformation("DashboardService received message on '{RoutingKey}': {Payload}", routingKey, json);

                        if (routingKey.Equals(RabbitMqConstants.RoutingKeyRegistered, StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = JsonSerializer.Deserialize<ParticipantRegisteredMessage>(json);
                            if (msg != null)
                            {
                                await _stateService.HandleParticipantRegisteredAsync(msg);
                            }
                        }
                        else if (routingKey.Equals(RabbitMqConstants.RoutingKeyCancelled, StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = JsonSerializer.Deserialize<ParticipantCancelledMessage>(json);
                            if (msg != null)
                            {
                                await _stateService.HandleParticipantCancelledAsync(msg);
                            }
                        }
                        else if (routingKey.Equals(RabbitMqConstants.RoutingKeyCheckedIn, StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = JsonSerializer.Deserialize<ParticipantCheckedInMessage>(json);
                            if (msg != null)
                            {
                                await _stateService.HandleParticipantCheckedInAsync(msg);
                            }
                        }

                        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing RabbitMQ message in DashboardService");
                        // Negative acknowledge and requeue on transient error
                        await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: RabbitMqConstants.DashboardQueue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("DashboardService is actively listening for event messages...");

                // Wait until cancellation is requested or connection drops
                while (!stoppingToken.IsCancellationRequested && connection.IsOpen)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("RabbitMQ connection error in DashboardService ({Msg}). Re-trying in 5 seconds...", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
