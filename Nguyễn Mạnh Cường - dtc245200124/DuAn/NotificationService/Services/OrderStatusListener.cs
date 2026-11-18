using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts;
using System.Text.Json;

namespace NotificationService.Services;

public sealed class OrderStatusListener : BackgroundService
{
    public const string StatusQueue = "order-status-changed";
    private readonly IConfiguration _configuration;
    private readonly IHubContext<OrderHub> _hub;
    private readonly ILogger<OrderStatusListener> _logger;

    public OrderStatusListener(
        IConfiguration configuration,
        IHubContext<OrderHub> hub,
        ILogger<OrderStatusListener> logger)
    {
        _configuration = configuration;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
                    UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest"
                };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclare(StatusQueue, true, false, false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (_, delivery) =>
                {
                    try
                    {
                        var message = JsonSerializer.Deserialize<OrderStatusChangedMessage>(delivery.Body.ToArray());
                        if (message is null) throw new InvalidOperationException("Invalid status event.");
                        _hub.Clients.All.SendAsync("OrderStatusChanged", message, stoppingToken).GetAwaiter().GetResult();
                        channel.BasicAck(delivery.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process status event");
                        channel.BasicNack(delivery.DeliveryTag, false, true);
                    }
                };
                channel.BasicConsume(StatusQueue, false, consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Waiting for RabbitMQ");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
