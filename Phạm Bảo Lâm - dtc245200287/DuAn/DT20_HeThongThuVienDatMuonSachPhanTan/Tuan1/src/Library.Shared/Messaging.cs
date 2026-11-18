using RabbitMQ.Client;

namespace Library.Shared;

public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}

// Message mà API gửi sang Worker
public record ReservationRequested(Guid ReservationId, int UserId, int BookId, DateTime RequestedAt);

public static class RabbitMqTopology
{
    public const string RequestQueue = "reservation.requests";
    public const string DeadLetterQueue = "reservation.requests.dlq";

    // Api và Worker cùng gọi hàm này để khai báo queue giống hệt nhau
    public static async Task DeclareAsync(IChannel channel, CancellationToken ct = default)
    {
        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: ct);

        await channel.QueueDeclareAsync(RequestQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",                        // default exchange
                ["x-dead-letter-routing-key"] = DeadLetterQueue         // message bị nack -> sang DLQ
            }, cancellationToken: ct);
    }
}
