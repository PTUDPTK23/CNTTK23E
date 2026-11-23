using System.Text.Json;
using Library.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Library.Worker;

public sealed class ReservationConsumer(IOptions<RabbitMqSettings> options,
    IServiceScopeFactory scopes, ILogger<ReservationConsumer> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var s = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = s.Host, Port = s.Port, UserName = s.User, Password = s.Password,
            AutomaticRecoveryEnabled = true,                 // RabbitMQ rớt rồi lên lại -> tự nối lại
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        // RabbitMQ có thể chưa sẵn sàng lúc khởi động -> thử lại cho đến khi được
        IConnection connection;
        while (true)
        {
            try { connection = await factory.CreateConnectionAsync(ct); break; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogWarning("RabbitMQ chưa sẵn sàng ({Msg}), thử lại sau 5s", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }

        await using var conn = connection;
        await using var channel = await conn.CreateChannelAsync(cancellationToken: ct);
        await RabbitMqTopology.DeclareAsync(channel, ct);
        await channel.BasicQosAsync(0, 1, false, ct);        // mỗi lần chỉ nhận 1 message

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => HandleAsync(channel, ea, ct);
        await channel.BasicConsumeAsync(RabbitMqTopology.RequestQueue, autoAck: false, consumer, ct);

        log.LogInformation("Worker sẵn sàng, đang lắng nghe queue {Queue}", RabbitMqTopology.RequestQueue);
        await Task.Delay(Timeout.Infinite, ct);
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var processed = false;
        try
        {
            var msg = JsonSerializer.Deserialize<ReservationRequested>(ea.Body.Span)
                      ?? throw new JsonException("Message rỗng");
            await ProcessWithRetryAsync(msg, ct);
            processed = true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;   // đang tắt worker: không ack, RabbitMQ sẽ giao lại message
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Message thất bại, chuyển vào DLQ");
        }

        try
        {
            if (processed) await channel.BasicAckAsync(ea.DeliveryTag, false, ct);   // xử lý xong mới ack
            else await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct); // -> DLQ
        }
        catch (Exception ex)
        {
            // Kết nối rớt đúng lúc ack: message chưa được ack nên sẽ được giao lại.
            // Nhờ kiểm tra idempotent trong ProcessAsync nên không bị xử lý hai lần.
            log.LogWarning(ex, "Không gửi được ack/nack, message sẽ được giao lại");
        }
    }

    private async Task ProcessWithRetryAsync(ReservationRequested msg, CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                log.LogInformation("Nhận yêu cầu {ReservationId} (lần {Attempt})", msg.ReservationId, attempt);
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
                await ProcessAsync(db, msg, ct);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                log.LogWarning(ex, "Xử lý lỗi, thử lại lần {Attempt}", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), ct);
            }
        }
    }

    private async Task ProcessAsync(LibraryDbContext db, ReservationRequested msg, CancellationToken ct)
    {
        var r = await db.Reservations.FirstOrDefaultAsync(x => x.Id == msg.ReservationId, ct);
        if (r is null)
        {
            log.LogWarning("Không thấy reservation {ReservationId}, bỏ qua", msg.ReservationId);
            return;
        }

        // Idempotent: message bị giao lại (worker chết trước khi ack) thì không xử lý lần 2
        if (r.Status != ReservationStatus.Pending)
        {
            log.LogInformation("Reservation {ReservationId} đã ở trạng thái {Status}, bỏ qua", r.Id, r.Status);
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var duplicate = await db.Reservations.AnyAsync(x =>
            x.UserId == r.UserId && x.BookId == r.BookId && x.Id != r.Id &&
            (x.Status == ReservationStatus.Reserved || x.Status == ReservationStatus.Borrowed), ct);

        if (duplicate)
        {
            r.Status = ReservationStatus.Rejected;
            r.Reason = "Người dùng đã đặt hoặc đang mượn cuốn sách này";
        }
        else
        {
            // Trừ 1 bản NGUYÊN TỬ: chỉ trừ khi còn > 0, không bao giờ âm dù nhiều worker chạy song song
            var affected = await db.Books
                .Where(b => b.Id == r.BookId && b.AvailableCopies > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.AvailableCopies, b => b.AvailableCopies - 1), ct);

            if (affected == 1) r.Status = ReservationStatus.Reserved;
            else { r.Status = ReservationStatus.Rejected; r.Reason = "Hết bản khả dụng"; }
        }

        r.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        log.LogInformation("Reservation {ReservationId} -> {Status} {Reason}", r.Id, r.Status, r.Reason);
    }
}
