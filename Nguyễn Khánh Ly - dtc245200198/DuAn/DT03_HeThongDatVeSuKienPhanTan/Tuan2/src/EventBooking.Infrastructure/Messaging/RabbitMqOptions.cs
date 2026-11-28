namespace EventBooking.Infrastructure.Messaging;

/// <summary>
/// Cấu hình kết nối tới RabbitMQ và tên hàng đợi xử lý đặt vé.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5673;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public string BookingQueueName { get; set; } = "booking-requests";

    public int PublishTimeoutSeconds { get; set; } = 5;
}
