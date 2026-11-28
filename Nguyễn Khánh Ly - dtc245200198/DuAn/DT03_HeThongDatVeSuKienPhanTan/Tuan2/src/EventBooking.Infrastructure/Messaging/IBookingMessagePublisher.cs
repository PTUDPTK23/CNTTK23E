namespace EventBooking.Infrastructure.Messaging;

/// <summary>
/// Gửi message yêu cầu đặt vé vào hàng đợi RabbitMQ và trả về kết quả Publisher Confirms.
/// </summary>
public interface IBookingMessagePublisher
{
    Task<BookingPublishResult> PublishBookingRequestedAsync(
        BookingRequestedMessage message,
        CancellationToken cancellationToken = default);
}
