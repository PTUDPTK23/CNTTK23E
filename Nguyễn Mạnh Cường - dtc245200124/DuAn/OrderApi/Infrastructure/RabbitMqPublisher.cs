using RabbitMQ.Client;
using Shared.Contracts;
using System.Text;
using System.Text.Json;

namespace OrderApi.Infrastructure;

public sealed class RabbitMqPublisher : IDisposable
{
    public const string QueueName = "order-processing";
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
    }

    public void Publish(ProcessOrderMessage message)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        _channel.BasicPublish(exchange: "", routingKey: QueueName, basicProperties: properties, body: body);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
