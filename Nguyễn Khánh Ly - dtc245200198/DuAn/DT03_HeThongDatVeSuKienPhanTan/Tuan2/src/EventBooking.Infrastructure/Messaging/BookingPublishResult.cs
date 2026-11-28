namespace EventBooking.Infrastructure.Messaging;

/// <summary>
/// Kết quả gửi message lên RabbitMQ kèm trạng thái Publisher Confirms.
/// </summary>
public sealed record BookingPublishResult(
    bool IsConfirmed,
    bool IsReturned = false,
    string? ErrorMessage = null)
{
    public static BookingPublishResult Confirmed() => new(IsConfirmed: true);

    public static BookingPublishResult Failed(string errorMessage, bool isReturned = false) =>
        new(IsConfirmed: false, IsReturned: isReturned, ErrorMessage: errorMessage);
}
