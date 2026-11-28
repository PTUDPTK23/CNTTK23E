using System.Net.Sockets;
using EventBooking.Infrastructure.Data;
using EventBooking.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Booking.Worker.Services;

public sealed class BookingProcessor : IBookingProcessor
{
    private const string PendingStatus = "Pending";
    private const string ConfirmedStatus = "Confirmed";
    private const string RejectedStatus = "Rejected";

    private readonly AppDbContext _context;
    private readonly ILogger<BookingProcessor> _logger;

    public BookingProcessor(
        AppDbContext context,
        ILogger<BookingProcessor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BookingProcessDecision> ProcessAsync(
        BookingRequestedMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Khóa dòng Booking trong PostgreSQL để chống xử lý trùng đồng thời và lấy dữ liệu gốc từ DB
            var booking = await _context.Bookings
                .FromSqlInterpolated($"""
                    SELECT "Id", "UserId", "EventId", "Quantity", "Status", "CreatedAt"
                    FROM "Bookings"
                    WHERE "Id" = {message.BookingId}
                    FOR UPDATE
                    """)
                .FirstOrDefaultAsync(cancellationToken);

            if (booking is null)
            {
                _logger.LogError(
                    "Booking #{BookingId} does not exist in PostgreSQL. Message will not be requeued.",
                    message.BookingId);

                await SafeRollbackAsync(transaction, originalException: null);
                return BookingProcessDecision.RejectWithoutRequeue;
            }

            if (message.UserId != booking.UserId ||
                message.EventId != booking.EventId ||
                message.Quantity != booking.Quantity)
            {
                _logger.LogWarning(
                    "Message metadata mismatch for Booking #{BookingId}. Message(UserId={MessageUserId}, EventId={MessageEventId}, Quantity={MessageQuantity}) vs Database(UserId={DbUserId}, EventId={DbEventId}, Quantity={DbQuantity}). Using database values.",
                    booking.Id,
                    message.UserId,
                    message.EventId,
                    message.Quantity,
                    booking.UserId,
                    booking.EventId,
                    booking.Quantity);
            }

            if (string.Equals(booking.Status, ConfirmedStatus, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(booking.Status, RejectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Booking #{BookingId} is already in status '{Status}'. Skipping duplicate message.",
                    booking.Id,
                    booking.Status);

                await SafeRollbackAsync(transaction, originalException: null);
                return BookingProcessDecision.Acknowledge;
            }

            if (!string.Equals(booking.Status, PendingStatus, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Booking #{BookingId} has invalid status '{Status}' (expected Pending, Confirmed, or Rejected). Rejecting message without requeue to avoid infinite loop.",
                    booking.Id,
                    booking.Status);

                await SafeRollbackAsync(transaction, originalException: null);
                return BookingProcessDecision.RejectWithoutRequeue;
            }

            if (booking.Quantity <= 0)
            {
                _logger.LogError(
                    "Booking #{BookingId} has invalid Quantity={Quantity} in database. Updating status to Rejected.",
                    booking.Id,
                    booking.Quantity);

                booking.Status = RejectedStatus;
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return BookingProcessDecision.Acknowledge;
            }

            // Trừ vé nguyên tử trực tiếp trên PostgreSQL bằng EventId và Quantity từ BookingEntity
            var affectedRows = await _context.Events
                .Where(e => e.Id == booking.EventId && e.AvailableTickets >= booking.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        e => e.AvailableTickets,
                        e => e.AvailableTickets - booking.Quantity),
                    cancellationToken);

            if (affectedRows == 1)
            {
                booking.Status = ConfirmedStatus;
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Booking #{BookingId} confirmed. Deducted {Quantity} ticket(s) from Event #{EventId}.",
                    booking.Id,
                    booking.Quantity,
                    booking.EventId);

                return BookingProcessDecision.Acknowledge;
            }

            booking.Status = RejectedStatus;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogWarning(
                "Booking #{BookingId} rejected due to insufficient tickets or missing Event #{EventId} (requested Quantity={Quantity}).",
                booking.Id,
                booking.EventId,
                booking.Quantity);

            return BookingProcessDecision.Acknowledge;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await SafeRollbackAsync(transaction, originalException: null);
            throw;
        }
        catch (Exception ex) when (IsTransientException(ex, cancellationToken))
        {
            _logger.LogError(
                ex,
                "Transient PostgreSQL or connectivity error while processing Booking #{BookingId}. Rolling back and scheduling requeue.",
                message.BookingId);

            await SafeRollbackAsync(transaction, ex);
            return BookingProcessDecision.RequeueWithDelay;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Non-transient data or business processing error for Booking #{BookingId}. Rolling back and rejecting without requeue.",
                message.BookingId);

            await SafeRollbackAsync(transaction, ex);
            return BookingProcessDecision.RejectWithoutRequeue;
        }
        finally
        {
            if (transaction is not null)
            {
                try
                {
                    await transaction.DisposeAsync();
                }
                catch (Exception disposeEx)
                {
                    _logger.LogDebug(disposeEx, "Ignored error while disposing PostgreSQL transaction for Booking #{BookingId}.", message.BookingId);
                }
            }
        }
    }

    private static bool IsTransientException(Exception exception, CancellationToken callerToken)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case PostgresException pgEx:
                    return pgEx.IsTransient;

                case NpgsqlException npgsqlEx:
                    return npgsqlEx.IsTransient;

                case DbUpdateConcurrencyException:
                case TimeoutException:
                case SocketException:
                case IOException:
                    return true;

                case OperationCanceledException when !callerToken.IsCancellationRequested:
                    return true;
            }
        }

        return false;
    }

    private async Task SafeRollbackAsync(IDbContextTransaction? transaction, Exception? originalException)
    {
        if (transaction is null)
        {
            return;
        }

        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception rollbackEx)
        {
            if (originalException is not null)
            {
                _logger.LogWarning(
                    rollbackEx,
                    "PostgreSQL rollback failed after original error ({OriginalErrorType}: {OriginalMessage}). Original error is preserved.",
                    originalException.GetType().Name,
                    originalException.Message);
            }
            else
            {
                _logger.LogWarning(rollbackEx, "PostgreSQL rollback failed.");
            }
        }
    }
}
