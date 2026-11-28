using EventBooking.Infrastructure.Data;
using EventBooking.Infrastructure.Entities;
using EventBooking.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Ticket.API.DTOs.Bookings;

namespace Ticket.API.Controllers;

[ApiController]
[Route("bookings")]
public class BookingsController : ControllerBase
{
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private static readonly TimeSpan IdempotencyCacheTtl = TimeSpan.FromMinutes(30);

    private readonly AppDbContext _context;
    private readonly IBookingMessagePublisher _bookingMessagePublisher;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        AppDbContext context,
        IBookingMessagePublisher bookingMessagePublisher,
        IMemoryCache memoryCache,
        ILogger<BookingsController> logger)
    {
        _context = context;
        _bookingMessagePublisher = bookingMessagePublisher;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    // POST /bookings
    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(
        CreateBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new
            {
                message = "Số lượng vé phải lớn hơn 0."
            });
        }

        var idempotencyKey = Request.Headers[IdempotencyHeaderName].FirstOrDefault()?.Trim();
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingResult = await TryHandleIdempotentRequestAsync(
                idempotencyKey,
                request,
                cancellationToken);

            if (existingResult is not null)
            {
                return existingResult;
            }
        }

        // Kiểm tra người dùng
        var userExists = await _context.Users
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            return NotFound(new
            {
                message = "Không tìm thấy người dùng."
            });
        }

        // Kiểm tra sự kiện
        var eventExists = await _context.Events
            .AnyAsync(e => e.Id == request.EventId, cancellationToken);

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
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _memoryCache.Set(
                BuildIdempotencyCacheKey(idempotencyKey),
                new IdempotentBookingRecord(booking.Id, booking.UserId, booking.EventId, booking.Quantity),
                IdempotencyCacheTtl);
        }

        return await PublishAndBuildResponseAsync(
            booking,
            successStatusCode: StatusCodes.Status201Created,
            cancellationToken: cancellationToken);
    }

    // POST /bookings/{id}/republish
    [HttpPost("{id:int}/republish")]
    public async Task<ActionResult<BookingResponseDto>> RepublishPendingBooking(
        int id,
        CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy yêu cầu đặt vé."
            });
        }

        if (!string.Equals(booking.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new
            {
                message = $"Booking #{booking.Id} đã ở trạng thái '{booking.Status}', không cần gửi lại hàng đợi.",
                booking = MapToResponseDto(booking)
            });
        }

        return await PublishAndBuildResponseAsync(
            booking,
            successStatusCode: StatusCodes.Status200OK,
            cancellationToken: cancellationToken);
    }

    // GET /bookings/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponseDto>> GetBookingById(
        int id,
        CancellationToken cancellationToken)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy yêu cầu đặt vé."
            });
        }

        return Ok(booking);
    }

    private async Task<ActionResult<BookingResponseDto>?> TryHandleIdempotentRequestAsync(
        string idempotencyKey,
        CreateBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildIdempotencyCacheKey(idempotencyKey);
        if (!_memoryCache.TryGetValue<IdempotentBookingRecord>(cacheKey, out var cached) || cached is null)
        {
            return null;
        }

        if (cached.UserId != request.UserId ||
            cached.EventId != request.EventId ||
            cached.Quantity != request.Quantity)
        {
            return Conflict(new
            {
                message = "Idempotency-Key đã được sử dụng cho một yêu cầu đặt vé khác."
            });
        }

        var existingBooking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == cached.BookingId, cancellationToken);

        if (existingBooking is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy yêu cầu đặt vé đã lưu trước đó."
            });
        }

        if (!string.Equals(existingBooking.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(MapToResponseDto(existingBooking));
        }

        return await PublishAndBuildResponseAsync(
            existingBooking,
            successStatusCode: StatusCodes.Status200OK,
            cancellationToken: cancellationToken);
    }

    private async Task<ActionResult<BookingResponseDto>> PublishAndBuildResponseAsync(
        BookingEntity booking,
        int successStatusCode,
        CancellationToken cancellationToken)
    {
        var message = new BookingRequestedMessage
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            EventId = booking.EventId,
            Quantity = booking.Quantity,
            CreatedAt = booking.CreatedAt
        };

        BookingPublishResult? publishResult = null;
        try
        {
            publishResult = await _bookingMessagePublisher.PublishBookingRequestedAsync(message, cancellationToken);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Request cancelled while publishing BookingId {BookingId} to RabbitMQ. Keeping booking in Pending state.",
                booking.Id);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Yêu cầu bị hủy trước khi RabbitMQ xác nhận message. Booking được giữ nguyên ở trạng thái Pending.",
                bookingId = booking.Id,
                status = booking.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while publishing BookingId {BookingId} to RabbitMQ. Keeping booking in Pending state.",
                booking.Id);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Không thể xác nhận gửi message tới RabbitMQ. Booking được giữ nguyên ở trạng thái Pending.",
                bookingId = booking.Id,
                status = booking.Status,
                error = ex.Message
            });
        }

        if (publishResult is null || !publishResult.IsConfirmed)
        {
            var reason = publishResult?.ErrorMessage ?? "Không nhận được kết quả xác nhận từ RabbitMQ.";
            _logger.LogError(
                "RabbitMQ did not confirm message for BookingId {BookingId}. IsReturned: {IsReturned}, Reason: {Reason}",
                booking.Id,
                publishResult?.IsReturned,
                reason);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Booking đã được lưu ở trạng thái Pending nhưng RabbitMQ chưa xác nhận message.",
                bookingId = booking.Id,
                status = booking.Status,
                isReturned = publishResult?.IsReturned ?? false,
                error = reason
            });
        }

        return StatusCode(successStatusCode, MapToResponseDto(booking));
    }

    private static BookingResponseDto MapToResponseDto(BookingEntity booking) => new()
    {
        Id = booking.Id,
        UserId = booking.UserId,
        EventId = booking.EventId,
        Quantity = booking.Quantity,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt
    };

    private static string BuildIdempotencyCacheKey(string idempotencyKey) =>
        $"booking:idempotency:{idempotencyKey}";

    private sealed record IdempotentBookingRecord(
        int BookingId,
        int UserId,
        int EventId,
        int Quantity);
}
