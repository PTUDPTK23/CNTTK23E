using EventBooking.Infrastructure.Data;
using EventBooking.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.API.DTOs.Bookings;

namespace Ticket.API.Controllers;

[ApiController]
[Route("bookings")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BookingsController(AppDbContext context)
    {
        _context = context;
    }

    // POST /bookings
    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(
        CreateBookingRequestDto request)
    {
        // Kiểm tra số lượng vé
        if (request.Quantity <= 0)
        {
            return BadRequest(new
            {
                message = "Số lượng vé phải lớn hơn 0."
            });
        }

        // Kiểm tra người dùng
        var userExists = await _context.Users
            .AnyAsync(u => u.Id == request.UserId);

        if (!userExists)
        {
            return NotFound(new
            {
                message = "Không tìm thấy người dùng."
            });
        }

        // Kiểm tra sự kiện
        var eventExists = await _context.Events
            .AnyAsync(e => e.Id == request.EventId);

        if (!eventExists)
        {
            return NotFound(new
            {
                message = "Không tìm thấy sự kiện."
            });
        }

        // Tạo yêu cầu đặt vé với trạng thái Pending
        var booking = new BookingEntity
        {
            UserId = request.UserId,
            EventId = request.EventId,
            Quantity = request.Quantity,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        // Tạo dữ liệu phản hồi
        var response = new BookingResponseDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            EventId = booking.EventId,
            Quantity = booking.Quantity,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }
    // GET /bookings/{id}
[HttpGet("{id:int}")]
public async Task<ActionResult<BookingResponseDto>> GetBookingById(int id)
{
    var booking = await _context.Bookings
        .AsNoTracking()
        .Where(b => b.Id == id)
        .Select(b => new BookingResponseDto
        {
            Id = b.Id,
            UserId = b.UserId,
            EventId = b.EventId,
            Quantity = b.Quantity,
            Status = b.Status,
            CreatedAt = b.CreatedAt
        })
        .FirstOrDefaultAsync();

    if (booking == null)
    {
        return NotFound(new
        {
            message = "Không tìm thấy yêu cầu đặt vé."
        });
    }

    return Ok(booking);
}
}