using EventBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.API.DTOs.Events;

namespace Ticket.API.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventsController(AppDbContext context)
    {
        _context = context;
    }

    // GET /events
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetEvents()
    {
        var events = await _context.Events
            .AsNoTracking()
            .Select(e => new EventResponseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Location = e.Location,
                EventDate = e.EventDate,
                TotalTickets = e.TotalTickets,
                AvailableTickets = e.AvailableTickets
            })
            .ToListAsync();

        return Ok(events);
    }
    // GET /events/{id}
[HttpGet("{id:int}")]
public async Task<ActionResult<EventResponseDto>> GetEventById(int id)
{
    var eventItem = await _context.Events
        .AsNoTracking()
        .Where(e => e.Id == id)
        .Select(e => new EventResponseDto
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Location = e.Location,
            EventDate = e.EventDate,
            TotalTickets = e.TotalTickets,
            AvailableTickets = e.AvailableTickets
        })
        .FirstOrDefaultAsync();

    if (eventItem == null)
    {
        return NotFound(new
        {
            message = "Không tìm thấy sự kiện."
        });
    }

    return Ok(eventItem);
}
}