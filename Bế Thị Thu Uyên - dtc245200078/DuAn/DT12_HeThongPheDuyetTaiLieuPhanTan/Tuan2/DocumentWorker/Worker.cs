using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _cfg;

    public Worker(ILogger<Worker> logger, IConfiguration cfg)
    {
        _logger = logger;
        _cfg = cfg;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _cfg["RabbitMQ:Host"] ?? "localhost";
        var connStr = _cfg.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=documentdb;Username=postgres;Password=postgres";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = host };
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync("document_events", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                // Fair Dispatch: Cân bằng tải khi chạy nhiều Worker Node song song
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                _logger.LogInformation("[Worker Node: {Node}] Đã kết nối RabbitMQ & sẵn sàng nhận sự kiện...", Environment.MachineName);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    using var docJson = JsonDocument.Parse(json);
                    var eventId = docJson.RootElement.TryGetProperty("EventId", out var idProp)
                        ? idProp.GetGuid()
                        : Guid.NewGuid();

                    var options = new DbContextOptionsBuilder<WorkerDbContext>().UseNpgsql(connStr).Options;
                    await using var db = new WorkerDbContext(options);

                    // Kiểm tra Idempotency: Bỏ qua nếu EventId đã từng được xử lý
                    if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId, stoppingToken))
                    {
                        _logger.LogWarning("[Idempotency] Bỏ qua sự kiện trùng lặp EventId={EventId}", eventId);
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                        return;
                    }

                    _logger.LogInformation("==================================================");
                    _logger.LogInformation("[Worker: {Node}] Xử lý sự kiện mới: {Payload}", Environment.MachineName, json);

                    db.ProcessedEvents.Add(new ProcessedEventRecord
                    {
                        EventId = eventId,
                        ConsumerNode = Environment.MachineName,
                        ProcessedAt = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync(stoppingToken);

                    // Manual ACK sau khi ghi nhận hoàn tất xuống CSDL
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    _logger.LogInformation("==================================================");
                };

                await channel.BasicConsumeAsync(queue: "document_events", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("[Notification Worker] Chưa kết nối được RabbitMQ ({Msg}). Thử lại sau 5 giây...", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

public class ProcessedEventRecord
{
    public Guid EventId { get; set; }
    public string ConsumerNode { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public class WorkerDbContext : DbContext
{
    public WorkerDbContext(DbContextOptions<WorkerDbContext> options) : base(options) { }
    public DbSet<ProcessedEventRecord> ProcessedEvents => Set<ProcessedEventRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<ProcessedEventRecord>().HasKey(x => x.EventId);
}