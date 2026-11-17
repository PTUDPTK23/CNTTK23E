
namespace EventBooking.Infrastructure.Entities;

public class BookingEntity
{
    public int Id { get; set; }

    // Mã người đặt vé
    public int UserId { get; set; }

    // Mã sự kiện
    public int EventId { get; set; }

    // Số lượng vé yêu cầu
    public int Quantity { get; set; }

    // Trạng thái xử lý: Pending, Confirmed, Rejected
    public string Status { get; set; } = "Pending";

    // Thời điểm gửi yêu cầu đặt vé
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Thông tin người đặt vé
    public UserEntity User { get; set; } = null!;

    // Thông tin sự kiện
    public EventEntity Event { get; set; } = null!;
}
