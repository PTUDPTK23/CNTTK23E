using RabbitMQ.Client;

namespace EventBooking.Infrastructure.Messaging;

/// <summary>
/// Quản lý kết nối và tạo channel bất đồng bộ tới RabbitMQ.
/// </summary>
public interface IRabbitMqConnectionProvider : IAsyncDisposable
{
    Task<IChannel> CreateChannelAsync(
        CreateChannelOptions? options = null,
        CancellationToken cancellationToken = default);

    Task EnsureBookingQueueDeclaredAsync(IChannel channel, CancellationToken cancellationToken = default);
}
