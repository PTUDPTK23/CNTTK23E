namespace Ticket.API.DTOs.Users;

public class UserBookingResponseDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
