using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Notification.Shared;

public sealed class Broker(IConfiguration config)
{
    public const string Exchange = "notifications.commands";
    public const string Queue = "notification.jobs";
    public const string RoutingKey = "notification.send";
    public const string DeadExchange = "notifications.dead";
    public const string DeadQueue = "notification.dead";
    public const string AuditExchange = "notifications.events";
    public const string AuditQueue = "notification.audit";

    public async Task<IConnection> ConnectAsync(string name, CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            Port = config.GetValue("RabbitMQ:Port", 5672),
            UserName = config["RabbitMQ:Username"] ?? throw new InvalidOperationException("Thiếu RabbitMQ__Username."),
            Password = config["RabbitMQ:Password"] ?? throw new InvalidOperationException("Thiếu RabbitMQ__Password."),
            AutomaticRecoveryEnabled = false,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(15)
        };
        // Vòng lặp của service tự tạo lại connection/channel/topology khi có lỗi.
        return await factory.CreateConnectionAsync(name, ct);
    }

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(DeadExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(DeadQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(DeadQueue, DeadExchange, "invalid", cancellationToken: ct);
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadExchange,
                ["x-dead-letter-routing-key"] = "invalid"
            }, cancellationToken: ct);
        await channel.QueueBindAsync(Queue, Exchange, RoutingKey, cancellationToken: ct);
    }

    public static async Task DeclareAuditAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(AuditExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(AuditQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(AuditQueue, AuditExchange, "delivery.*", cancellationToken: ct);
    }
}
