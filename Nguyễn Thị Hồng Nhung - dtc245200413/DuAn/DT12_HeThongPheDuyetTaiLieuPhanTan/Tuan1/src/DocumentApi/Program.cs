using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentApi.Data;
using DocumentApi.Models;
using DocumentApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCors();

// Correlation ID Middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N")[..12];
    }

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});

// Ensure database is created with retry logic for container startup resilience
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (int attempt = 1; attempt <= 15; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("DocumentDbContext initialized successfully.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Database not ready yet (attempt {Attempt}/15): {Error}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

static string GetCorrelationId(HttpContext ctx) =>
    ctx.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..12];

static async Task<WorkflowActionRequest> ReadActionRequestAsync(HttpRequest request)
{
    if (request.ContentLength is null or 0)
        return new WorkflowActionRequest(null, null);

    try
    {
        return await request.ReadFromJsonAsync<WorkflowActionRequest>()
               ?? new WorkflowActionRequest(null, null);
    }
    catch
    {
        return new WorkflowActionRequest(null, null);
    }
}

// Health & Outbox Diagnostics
app.MapGet("/health", () => Results.Ok(new
{
    service = "DocumentApi",
    status = "Healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/outbox/stats", async (DocumentDbContext db) =>
{
    var pending = await db.OutboxMessages.CountAsync(x => x.ProcessedAt == null);
    var processed = await db.OutboxMessages.CountAsync(x => x.ProcessedAt != null);
    return Results.Ok(new { pending, processed });
});

// 1. POST /documents - Tạo tài liệu mới ở trạng thái Draft
app.MapPost("/documents", async (CreateDocumentRequest req, DocumentDbContext db, HttpContext ctx, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(req.Title) ||
        string.IsNullOrWhiteSpace(req.CreatedBy) ||
        string.IsNullOrWhiteSpace(req.Approver))
    {
        return Results.BadRequest(new { message = "Title, CreatedBy và Approver là bắt buộc." });
    }

    var correlationId = GetCorrelationId(ctx);
    var now = DateTime.UtcNow;

    var doc = new DocumentItem
    {
        Id = Guid.NewGuid(),
        Title = req.Title.Trim(),
        Content = req.Content?.Trim() ?? string.Empty,
        Category = string.IsNullOrWhiteSpace(req.Category) ? "Hồ sơ chung" : req.Category.Trim(),
        CreatedBy = req.CreatedBy.Trim(),
        Approver = req.Approver.Trim(),
        Status = DocumentStatus.Draft,
        CreatedAt = now,
        UpdatedAt = now
    };

    doc.Histories.Add(new ApprovalHistory
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        Action = "Created",
        FromStatus = null,
        ToStatus = DocumentStatus.Draft,
        Actor = doc.CreatedBy,
        Comment = "Khởi tạo tài liệu nháp",
        CorrelationId = correlationId,
        Timestamp = now
    });

    db.Documents.Add(doc);
    await db.SaveChangesAsync();

    logger.LogInformation("[CorrelationId={CorrelationId}] Created document {DocumentId} ('{Title}') by {CreatedBy}.",
        correlationId, doc.Id, doc.Title, doc.CreatedBy);

    return Results.Created($"/documents/{doc.Id}", doc);
});

// 2. GET /documents - Tra cứu danh sách tài liệu theo trạng thái / người xử lý / người tạo
app.MapGet("/documents", async (
    string? status,
    string? approver,
    string? createdBy,
    string? search,
    DocumentDbContext db) =>
{
    var query = db.Documents
        .Include(d => d.Histories.OrderBy(h => h.Timestamp))
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(status))
    {
        if (!Enum.TryParse<DocumentStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            return Results.BadRequest(new { message = $"Trạng thái không hợp lệ: {status}. Hỗ trợ: Draft, Submitted, Approved, Rejected." });
        }
        query = query.Where(d => d.Status == parsedStatus);
    }

    if (!string.IsNullOrWhiteSpace(approver))
    {
        var appFilter = approver.Trim().ToLower();
        query = query.Where(d => d.Approver.ToLower().Contains(appFilter));
    }

    if (!string.IsNullOrWhiteSpace(createdBy))
    {
        var creatorFilter = createdBy.Trim().ToLower();
        query = query.Where(d => d.CreatedBy.ToLower().Contains(creatorFilter));
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var keyword = search.Trim().ToLower();
        query = query.Where(d => d.Title.ToLower().Contains(keyword) || d.Content.ToLower().Contains(keyword));
    }

    var results = await query
        .OrderByDescending(d => d.UpdatedAt)
        .ToListAsync();

    return Results.Ok(results);
});

// 3. GET /documents/{id} - Xem chi tiết tài liệu và lịch sử phê duyệt
app.MapGet("/documents/{id:guid}", async (Guid id, DocumentDbContext db) =>
{
    var doc = await db.Documents
        .Include(d => d.Histories.OrderBy(h => h.Timestamp))
        .FirstOrDefaultAsync(d => d.Id == id);

    return doc is null
        ? Results.NotFound(new { message = "Không tìm thấy tài liệu." })
        : Results.Ok(doc);
});

// 4. POST /documents/{id}/submit - Gửi tài liệu vào quy trình phê duyệt
app.MapPost("/documents/{id:guid}/submit", async (Guid id, HttpRequest request, DocumentDbContext db, HttpContext ctx, ILogger<Program> logger) =>
{
    var doc = await db.Documents.Include(d => d.Histories).FirstOrDefaultAsync(d => d.Id == id);
    if (doc is null) return Results.NotFound(new { message = "Không tìm thấy tài liệu." });

    if (doc.Status != DocumentStatus.Draft)
    {
        return Results.BadRequest(new { message = $"Chỉ có thể gửi duyệt tài liệu ở trạng thái Draft. Trạng thái hiện tại: {doc.Status}." });
    }

    var body = await ReadActionRequestAsync(request);
    var actor = string.IsNullOrWhiteSpace(body.Actor) ? doc.CreatedBy : body.Actor.Trim();
    var correlationId = GetCorrelationId(ctx);
    var now = DateTime.UtcNow;
    var oldStatus = doc.Status;

    doc.Status = DocumentStatus.Submitted;
    doc.UpdatedAt = now;

    db.ApprovalHistories.Add(new ApprovalHistory
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        Action = "Submitted",
        FromStatus = oldStatus,
        ToStatus = DocumentStatus.Submitted,
        Actor = actor,
        Comment = string.IsNullOrWhiteSpace(body.Comment) ? "Gửi tài liệu chờ phê duyệt" : body.Comment.Trim(),
        CorrelationId = correlationId,
        Timestamp = now
    });

    var eventId = Guid.NewGuid();
    var evt = new DocumentStatusChangedEvent(
        EventId: eventId,
        EventType: "DocumentSubmitted",
        DocumentId: doc.Id,
        Title: doc.Title,
        CreatedBy: doc.CreatedBy,
        Approver: doc.Approver,
        FromStatus: oldStatus.ToString(),
        ToStatus: doc.Status.ToString(),
        Actor: actor,
        Comment: body.Comment,
        CorrelationId: correlationId,
        OccurredAt: now
    );

    db.OutboxMessages.Add(new OutboxMessage
    {
        Id = eventId,
        EventType = evt.EventType,
        RoutingKey = "document.submitted",
        Payload = JsonSerializer.Serialize(evt),
        CorrelationId = correlationId,
        CreatedAt = now
    });

    await db.SaveChangesAsync();

    logger.LogInformation("[CorrelationId={CorrelationId}] Document {DocumentId} submitted by {Actor}.",
        correlationId, doc.Id, actor);

    return Results.Ok(doc);
});

// 5. POST /documents/{id}/approve - Phê duyệt tài liệu
app.MapPost("/documents/{id:guid}/approve", async (Guid id, HttpRequest request, DocumentDbContext db, HttpContext ctx, ILogger<Program> logger) =>
{
    var doc = await db.Documents.Include(d => d.Histories).FirstOrDefaultAsync(d => d.Id == id);
    if (doc is null) return Results.NotFound(new { message = "Không tìm thấy tài liệu." });

    if (doc.Status != DocumentStatus.Submitted)
    {
        return Results.BadRequest(new { message = $"Chỉ có thể phê duyệt tài liệu ở trạng thái Submitted. Trạng thái hiện tại: {doc.Status}." });
    }

    var body = await ReadActionRequestAsync(request);
    var actor = string.IsNullOrWhiteSpace(body.Actor) ? doc.Approver : body.Actor.Trim();
    var correlationId = GetCorrelationId(ctx);
    var now = DateTime.UtcNow;
    var oldStatus = doc.Status;

    doc.Status = DocumentStatus.Approved;
    doc.UpdatedAt = now;

    db.ApprovalHistories.Add(new ApprovalHistory
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        Action = "Approved",
        FromStatus = oldStatus,
        ToStatus = DocumentStatus.Approved,
        Actor = actor,
        Comment = string.IsNullOrWhiteSpace(body.Comment) ? "Đã phê duyệt tài liệu" : body.Comment.Trim(),
        CorrelationId = correlationId,
        Timestamp = now
    });

    var eventId = Guid.NewGuid();
    var evt = new DocumentStatusChangedEvent(
        EventId: eventId,
        EventType: "DocumentApproved",
        DocumentId: doc.Id,
        Title: doc.Title,
        CreatedBy: doc.CreatedBy,
        Approver: doc.Approver,
        FromStatus: oldStatus.ToString(),
        ToStatus: doc.Status.ToString(),
        Actor: actor,
        Comment: body.Comment,
        CorrelationId: correlationId,
        OccurredAt: now
    );

    db.OutboxMessages.Add(new OutboxMessage
    {
        Id = eventId,
        EventType = evt.EventType,
        RoutingKey = "document.approved",
        Payload = JsonSerializer.Serialize(evt),
        CorrelationId = correlationId,
        CreatedAt = now
    });

    await db.SaveChangesAsync();

    logger.LogInformation("[CorrelationId={CorrelationId}] Document {DocumentId} approved by {Actor}.",
        correlationId, doc.Id, actor);

    return Results.Ok(doc);
});

// 6. POST /documents/{id}/reject - Từ chối tài liệu
app.MapPost("/documents/{id:guid}/reject", async (Guid id, HttpRequest request, DocumentDbContext db, HttpContext ctx, ILogger<Program> logger) =>
{
    var doc = await db.Documents.Include(d => d.Histories).FirstOrDefaultAsync(d => d.Id == id);
    if (doc is null) return Results.NotFound(new { message = "Không tìm thấy tài liệu." });

    if (doc.Status != DocumentStatus.Submitted)
    {
        return Results.BadRequest(new { message = $"Chỉ có thể từ chối tài liệu ở trạng thái Submitted. Trạng thái hiện tại: {doc.Status}." });
    }

    var body = await ReadActionRequestAsync(request);
    var actor = string.IsNullOrWhiteSpace(body.Actor) ? doc.Approver : body.Actor.Trim();
    var correlationId = GetCorrelationId(ctx);
    var now = DateTime.UtcNow;
    var oldStatus = doc.Status;

    doc.Status = DocumentStatus.Rejected;
    doc.UpdatedAt = now;

    db.ApprovalHistories.Add(new ApprovalHistory
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        Action = "Rejected",
        FromStatus = oldStatus,
        ToStatus = DocumentStatus.Rejected,
        Actor = actor,
        Comment = string.IsNullOrWhiteSpace(body.Comment) ? "Từ chối phê duyệt tài liệu" : body.Comment.Trim(),
        CorrelationId = correlationId,
        Timestamp = now
    });

    var eventId = Guid.NewGuid();
    var evt = new DocumentStatusChangedEvent(
        EventId: eventId,
        EventType: "DocumentRejected",
        DocumentId: doc.Id,
        Title: doc.Title,
        CreatedBy: doc.CreatedBy,
        Approver: doc.Approver,
        FromStatus: oldStatus.ToString(),
        ToStatus: doc.Status.ToString(),
        Actor: actor,
        Comment: body.Comment,
        CorrelationId: correlationId,
        OccurredAt: now
    );

    db.OutboxMessages.Add(new OutboxMessage
    {
        Id = eventId,
        EventType = evt.EventType,
        RoutingKey = "document.rejected",
        Payload = JsonSerializer.Serialize(evt),
        CorrelationId = correlationId,
        CreatedAt = now
    });

    await db.SaveChangesAsync();

    logger.LogInformation("[CorrelationId={CorrelationId}] Document {DocumentId} rejected by {Actor}.",
        correlationId, doc.Id, actor);

    return Results.Ok(doc);
});

app.Run();
