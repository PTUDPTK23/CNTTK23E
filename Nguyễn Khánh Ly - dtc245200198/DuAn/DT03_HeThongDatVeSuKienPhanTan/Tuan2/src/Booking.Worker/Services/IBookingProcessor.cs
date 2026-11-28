using EventBooking.Infrastructure.Messaging;

namespace Booking.Worker.Services;

public enum BookingProcessDecision
{
    Acknowledge = 0,
    RejectWithoutRequeue = 1,
    RequeueWithDelay = 2
}

public interface IBookingProcessor
{
    Task<BookingProcessDecision> ProcessAsync(
        BookingRequestedMessage message,
        CancellationToken cancellationToken = default);
}
