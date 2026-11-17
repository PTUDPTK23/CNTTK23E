
namespace EventBooking.Infrastructure.Entities;

public class EventEntity
{
    public int Id { get; set; }

    // Tên sự kiện
    public string Name { get; set; } = string.Empty;

    // Mô tả sự kiện
    public string? Description { get; set; }

    // Địa điểm tổ chức
    public string Location { get; set; } = string.Empty;

    // Thời gian diễn ra sự kiện
    public DateTime EventDate { get; set; }

    // Tổng số vé ban đầu
    public int TotalTickets { get; set; }

    // Số vé còn lại
    public int AvailableTickets { get; set; }
}
