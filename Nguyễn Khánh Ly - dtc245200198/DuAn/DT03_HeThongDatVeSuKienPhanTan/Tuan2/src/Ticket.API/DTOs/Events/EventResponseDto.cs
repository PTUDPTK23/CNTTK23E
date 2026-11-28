
namespace Ticket.API.DTOs.Events;

public class EventResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Location { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public int TotalTickets { get; set; }

    public int AvailableTickets { get; set; }
}
