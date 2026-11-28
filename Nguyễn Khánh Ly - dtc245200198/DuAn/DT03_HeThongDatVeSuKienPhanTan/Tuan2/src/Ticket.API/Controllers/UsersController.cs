using EventBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket.API.DTOs.Users;

namespace Ticket.API.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // GET /users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    // GET /users/{userId}/bookings
    [HttpGet("{userId:int}/bookings")]
    public async Task<ActionResult<IEnumerable<UserBookingResponseDto>>> GetUserBookings(
        int userId,
        CancellationToken cancellationToken)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.Id == userId, cancellationToken);

        if (!userExists)
        {
            return NotFound(new
            {
                message = "Không tìm thấy người dùng."
            });
        }

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .Select(b => new UserBookingResponseDto
            {
                Id = b.Id,
                UserId = b.UserId,
                EventId = b.EventId,
                EventName = b.Event.Name,
                Quantity = b.Quantity,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(bookings);
    }
}
