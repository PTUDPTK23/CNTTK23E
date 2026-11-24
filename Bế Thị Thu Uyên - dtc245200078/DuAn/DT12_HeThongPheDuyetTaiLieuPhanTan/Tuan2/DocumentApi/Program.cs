using System.Text;
using System.Text.Json;
using DocumentApi.Data;
using DocumentApi.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=documentdb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<DocumentDbContext>(opt => opt.UseNpgsql(connStr));
builder.Services.AddHostedService<OutboxRelayService>(); // Background service đẩy Outbox sang RabbitMQ

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();

// Endpoint kiểm tra sức khỏe thực tế của cụm phân tán (API Node, PostgreSQL, RabbitMQ, Outbox)
app.MapGet("/cluster/health", async (DocumentDbContext db, IConfiguration cfg) =>
{
    var dbOk = await db.Database.CanConnectAsync();
    var pendingOutbox = await db.OutboxMessages.CountAsync(x => x.ProcessedAt == null);
    bool mqOk = false;
    try
    {
        var factory = new ConnectionFactory { HostName = cfg["RabbitMQ:Host"] ?? "localhost" };
        using var conn = await factory.CreateConnectionAsync();
        mqOk = conn.IsOpen;
    }
    catch { mqOk = false; }

    return Results.Ok(new
    {
        apiNode = Environment.MachineName,
        database = dbOk ? "Connected" : "Disconnected",
        rabbitMq = mqOk ? "Connected" : "Disconnected",
        pendingOutboxEvents = pendingOutbox,
        timestampUtc = DateTime.UtcNow
    });
});

app.MapGet("/documents", async (DocumentDbContext db) =>
    await db.Documents.OrderByDescending(d => d.Id).ToListAsync());

app.MapGet("/documents/{id:int}", async (int id, DocumentDbContext db) =>
{
    var doc = await db.Documents.FindAsync(id);
    if (doc is null) return Results.NotFound();
    var histories = await db.DocumentHistories
        .Where(h => h.DocumentId == id)
        .OrderByDescending(h => h.Timestamp)
        .ToListAsync();
    return Results.Ok(new { document = doc, histories });
});

app.MapPost("/documents", async (Document input, DocumentDbContext db) =>
{
    await using var tx = await db.Database.BeginTransactionAsync();

    input.Status = "Draft";
    input.Version = 1;
    input.CreatedAt = DateTime.UtcNow;
    input.UpdatedAt = DateTime.UtcNow;
    db.Documents.Add(input);
    await db.SaveChangesAsync();

    var eventId = Guid.NewGuid();
    db.DocumentHistories.Add(new DocumentHistory
    {
        DocumentId = input.Id,
        Action = "Tạo bản nháp",
        Actor = input.CreatedBy,
        Note = "Khởi tạo hồ sơ trên hệ thống",
        ProcessedByNode = Environment.MachineName
    });

    db.OutboxMessages.Add(new OutboxMessage
    {
        Id = eventId,
        EventType = "DocumentCreated",
        Payload = JsonSerializer.Serialize(new
        {
            EventId = eventId,
            Event = "DocumentCreated",
            input.Id,
            input.Title,
            input.Status,
            Actor = input.CreatedBy,
            Timestamp = DateTime.UtcNow
        })
    });

    await db.SaveChangesAsync();
    await tx.CommitAsync();
    return Results.Created($"/documents/{input.Id}", input);
});

app.MapPost("/documents/{id:int}/{action}", async (int id, string action, ActionRequest req, DocumentDbContext db) =>
{
    var doc = await db.Documents.FindAsync(id);
    if (doc is null) return Results.NotFound();

    var (nextStatus, actionLabel, eventName) = action.ToLower() switch
    {
        "submit" when doc.Status is "Draft" or "Rejected" => ("Submitted", "Gửi trình duyệt", "DocumentSubmitted"),
        "approve" when doc.Status == "Submitted" => ("Approved", "Phê duyệt hồ sơ", "DocumentApproved"),
        "reject" when doc.Status == "Submitted" => ("Rejected", "Từ chối hồ sơ", "DocumentRejected"),
        _ => (string.Empty, string.Empty, string.Empty)
    };

    if (string.IsNullOrEmpty(nextStatus))
        return Results.BadRequest($"Không thể thực hiện '{action}' khi tài liệu đang ở trạng thái '{doc.Status}'.");

    try
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        doc.Status = nextStatus;
        doc.Version += 1; // Tăng version để kiểm soát đồng thời lạc quan
        doc.UpdatedAt = DateTime.UtcNow;

        var eventId = Guid.NewGuid();
        db.DocumentHistories.Add(new DocumentHistory
        {
            DocumentId = doc.Id,
            Action = actionLabel,
            Actor = req.Actor,
            Note = req.Note,
            ProcessedByNode = Environment.MachineName
        });

        // Ghi vào Outbox trong cùng Transaction
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = eventId,
            EventType = eventName,
            Payload = JsonSerializer.Serialize(new
            {
                EventId = eventId,
                Event = eventName,
                doc.Id,
                doc.Title,
                doc.Status,
                doc.Version,
                req.Actor,
                doc.Approver,
                Timestamp = DateTime.UtcNow
            })
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Results.Ok(doc);
    }
    catch (DbUpdateConcurrencyException)
    {
        return Results.Conflict("Xung đột dữ liệu phân tán: Hồ sơ vừa được cập nhật bởi một phiên làm việc khác. Vui lòng tải lại!");
    }
});

app.Run();

public record ActionRequest(string Actor, string? Note);

// Background Service đọc OutboxMessages và đẩy tin cậy sang RabbitMQ
public class OutboxRelayService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<OutboxRelayService> _logger;

    public OutboxRelayService(IServiceProvider sp, IConfiguration cfg, ILogger<OutboxRelayService> logger)
    {
        _sp = sp; _cfg = cfg; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
                var pending = await db.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (pending.Count > 0)
                {
                    var factory = new ConnectionFactory { HostName = _cfg["RabbitMQ:Host"] ?? "localhost" };
                    await using var conn = await factory.CreateConnectionAsync(stoppingToken);
                    await using var channel = await conn.CreateChannelAsync(cancellationToken: stoppingToken);
                    await channel.QueueDeclareAsync("document_events", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

                    foreach (var msg in pending)
                    {
                        var body = Encoding.UTF8.GetBytes(msg.Payload);
                        await channel.BasicPublishAsync(exchange: "", routingKey: "document_events", body: body, cancellationToken: stoppingToken);
                        msg.ProcessedAt = DateTime.UtcNow;
                        _logger.LogInformation("[Outbox Relay] Đã phát sự kiện {EventType} ({Id}) tới RabbitMQ", msg.EventType, msg.Id);
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Outbox Relay] Chờ kết nối RabbitMQ: {Msg}", ex.Message);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}