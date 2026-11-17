namespace FileProcessing.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    public string ExchangeName { get; set; } = "file.processing.exchange";
    public string MainQueueName { get; set; } = "file.processing.main";
    public string RetryQueueName { get; set; } = "file.processing.retry";
    public string DlqQueueName { get; set; } = "file.processing.dlq";
    public string DlxExchangeName { get; set; } = "file.processing.dlx";
    public string RoutingKey { get; set; } = "image.process";
}
