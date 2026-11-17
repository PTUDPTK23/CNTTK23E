namespace Ticket.API.DTOs.Bookings;

public class CreateBookingRequestDto
{
    public int UserId { get; set; }
    public int EventId { get; set; }
    public int Quantity { get; set; }
}