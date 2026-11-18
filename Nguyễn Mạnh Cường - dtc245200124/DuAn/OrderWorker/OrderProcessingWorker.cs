using Microsoft.EntityFrameworkCore;
using OrderWorker.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts;
using System.Text;
using System.Text.Json;

namespace OrderWorker;

public sealed class OrderProcessingWorker : BackgroundService
{
    public const string ProcessingQueue = "order-processing";
    public const string StatusQueue = "order-status-changed";
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderProcessingWorker> _logger;

    public OrderProcessingWorker(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderProcessingWorker> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureDatabaseAsync(stoppingToken);
        var factory = CreateFactory();
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(ProcessingQueue, true, false, false);
        channel.QueueDeclare(StatusQueue, true, false, false);
        channel.BasicQos(0, 1, false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, delivery) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<ProcessOrderMessage>(delivery.Body.ToArray());
                if (message is null) throw new InvalidOperationException("Invalid processing message.");
                ProcessOrderAsync(message, channel).GetAwaiter().GetResult();
                channel.BasicAck(delivery.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process order");
                channel.BasicNack(delivery.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(ProcessingQueue, false, consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task EnsureDatabaseAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
                await db.Database.EnsureCreatedAsync(stoppingToken);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Waiting for PostgreSQL");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task ProcessOrderAsync(ProcessOrderMessage message, IModel channel)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == message.OrderId);
        if (order is null) throw new InvalidOperationException($"Order {message.OrderId} was not found.");

        await SetStatusAsync(db, order, OrderStatus.Processing, null, channel);
        await Task.Delay(TimeSpan.FromSeconds(3));

        var failed = order.Products.Any(p => p.Quantity > 100);
        if (failed)
        {
            await SetStatusAsync(db, order, OrderStatus.Failed, "Product quantity exceeds 100", channel);
            return;
        }

        await SetStatusAsync(db, order, OrderStatus.Completed, null, channel);
    }

    private async Task SetStatusAsync(
        WorkerDbContext db,
        Order order,
        OrderStatus status,
        string? reason,
        IModel channel)
    {
        order.Status = status;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var statusChanged = new OrderStatusChangedMessage(
            order.Id, order.CustomerName, status, order.UpdatedAt.Value, reason);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(statusChanged));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        channel.BasicPublish("", StatusQueue, properties, body);
    }

    private ConnectionFactory CreateFactory() => new()
    {
        HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
        Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
        UserName = _configuration["RabbitMQ:Username"] ?? "guest",
        Password = _configuration["RabbitMQ:Password"] ?? "guest"
    };
}
