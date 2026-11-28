namespace EventBooking.Infrastructure.Messaging;

/// <summary>
/// Message contract dùng chung giữa Ticket.API (Producer) và Booking.Worker (Consumer).
/// </summary>
public sealed record BookingRequestedMessage
{
    public int BookingId { get; init; }

    public int UserId { get; init; }

    public int EventId { get; init; }

    public int Quantity { get; init; }

    public DateTime CreatedAt { get; init; }
}
