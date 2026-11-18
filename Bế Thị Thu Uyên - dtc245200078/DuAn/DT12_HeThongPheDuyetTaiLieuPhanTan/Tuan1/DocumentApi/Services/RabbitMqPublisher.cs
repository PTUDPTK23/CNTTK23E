using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace DocumentApi.Services
{
    public class RabbitMqPublisher
    {
        private readonly string _hostName;
        private readonly string _userName;
        private readonly string _password;
        private const string QueueName = "document_events";

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _hostName = configuration["RabbitMQ:Host"] ?? "localhost";
            _userName = configuration["RabbitMQ:Username"] ?? "guest";
            _password = configuration["RabbitMQ:Password"] ?? "guest";
        }

        public async Task PublishEventAsync(object eventData)
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                UserName = _userName,
                Password = _password
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var message = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: QueueName,
                body: body);
        }
    }
}