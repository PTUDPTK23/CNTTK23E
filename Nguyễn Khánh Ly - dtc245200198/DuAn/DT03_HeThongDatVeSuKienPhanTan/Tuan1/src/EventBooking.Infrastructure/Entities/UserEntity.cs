
namespace EventBooking.Infrastructure.Entities;

public class UserEntity
{
    public int Id { get; set; }

    // Họ và tên người đặt vé
    public string FullName { get; set; } = string.Empty;

    // Email người đặt vé
    public string Email { get; set; } = string.Empty;

    // Danh sách yêu cầu đặt vé của người dùng
    public ICollection<BookingEntity> Bookings { get; set; }
        = new List<BookingEntity>();
}
