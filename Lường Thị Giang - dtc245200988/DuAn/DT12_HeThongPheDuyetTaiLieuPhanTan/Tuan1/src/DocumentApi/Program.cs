using DocumentApi.Data;
using DocumentApi.Models;
using DocumentApi.Services;
using Microsoft.EntityFrameworkCore;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chưa cấu hình DefaultConnection.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

var app = builder.Build();

app.UseCors();

// Correlation ID Middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N")[..12];
    }

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;

    using (app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});

// Tự động khởi tạo Database & dữ liệu mẫu khi khởi động container
await InitializeDatabaseAsync(app);

// Health Check Endpoint
app.MapGet("/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return Results.Ok(new
    {
        status = canConnect ? "Healthy" : "Degraded",
        service = "DocumentApi",
        databaseConnected = canConnect,
        timestampUtc = DateTime.UtcNow
    });
});

// 1. Tạo tài liệu mới (Draft)
app.MapPost("/documents", async (
    CreateDocumentRequest req,
    AppDbContext db,
    HttpContext httpContext,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(req.Title) ||
        string.IsNullOrWhiteSpace(req.Creator) ||
        string.IsNullOrWhiteSpace(req.Approver))
    {
        return Results.BadRequest(new { message = "Tiêu đề, người tạo và người phê duyệt không được để trống." });
    }

    var correlationId = httpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..12];
    var now = DateTime.UtcNow;

    var doc = new DocumentItem
    {
        Id = Guid.NewGuid(),
        Title = req.Title.Trim(),
        Content = req.Content?.Trim() ?? string.Empty,
        Category = string.IsNullOrWhiteSpace(req.Category) ? "Hồ sơ hành chính" : req.Category.Trim(),
        Creator = req.Creator.Trim(),
        Approver = req.Approver.Trim(),
        Status = DocumentStatuses.Draft,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    var history = new ApprovalHistory
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        Action = "Created",
        FromStatus = "None",
        ToStatus = DocumentStatuses.Draft,
        PerformedBy = doc.Creator,
        Comment = "Khởi tạo tài liệu nháp",
        CorrelationId = correlationId,
        TimestampUtc = now
    };

    db.Documents.Add(doc);
    db.ApprovalHistories.Add(history);
    await db.SaveChangesAsync();

    logger.LogInformation(
        "[CorrelationId={CorrelationId}] Tạo mới tài liệu {DocumentId} - '{Title}' bởi {Creator}",
        correlationId, doc.Id, doc.Title, doc.Creator);

    return Results.Created($"/documents/{doc.Id}", doc);
});

// 2. Tra cứu danh sách tài liệu (lọc theo trạng thái, người tạo, người phê duyệt, từ khóa)
app.MapGet("/documents", async (
    string? status,
    string? creator,
    string? approver,
    string? search,
    AppDbContext db) =>
{
    var query = db.Documents
        .Include(d => d.Histories)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(d => d.Status.ToLower() == status.Trim().ToLower());
    }

    if (!string.IsNullOrWhiteSpace(creator))
    {
        var c = creator.Trim().ToLower();
        query = query.Where(d => d.Creator.ToLower().Contains(c));
    }

    if (!string.IsNullOrWhiteSpace(approver))
    {
        var a = approver.Trim().ToLower();
        query = query.Where(d => d.Approver.ToLower().Contains(a));
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim().ToLower();
        query = query.Where(d =>
            d.Title.ToLower().Contains(s) ||
            d.Content.ToLower().Contains(s) ||
            d.Creator.ToLower().Contains(s) ||
            d.Approver.ToLower().Contains(s));
    }

    var docs = await query
        .OrderByDescending(d => d.UpdatedAtUtc)
        .ToListAsync();

    foreach (var d in docs)
    {
        d.Histories = d.Histories.OrderBy(h => h.TimestampUtc).ToList();
    }

    return Results.Ok(docs);
});

// 3. Xem chi tiết tài liệu và lịch sử xử lý
app.MapGet("/documents/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var doc = await db.Documents
        .Include(d => d.Histories)
        .FirstOrDefaultAsync(d => d.Id == id);

    if (doc is null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy tài liệu có ID = {id}" });
    }

    doc.Histories = doc.Histories.OrderBy(h => h.TimestampUtc).ToList();
    return Results.Ok(doc);
});

// 4. Gửi tài liệu vào quy trình phê duyệt (Draft / Rejected -> Submitted)
app.MapPost("/documents/{id:guid}/submit", async (
    Guid id,
    SubmitDocumentRequest? req,
    AppDbContext db,
    IEventPublisher publisher,
    HttpContext httpContext,
    ILogger<Program> logger) =>
{
    var doc = await db.Documents.Include(d => d.Histories).FirstOrDefaultAsync(d => d.Id == id);
    if (doc is null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy tài liệu có ID = {id}" });
    }

    if (doc.Status != DocumentStatuses.Draft && doc.Status != DocumentStatuses.Rejected)
    {
        return Results.BadRequest(new
        {
            message = $"Chỉ có thể gửi duyệt tài liệu ở trạng thái Draft hoặc Rejected. Trạng thái hiện tại: {doc.Status}."
        });
    }

    var correlationId = httpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..12];
    var oldStatus = doc.Status;
    var actor = string.IsNullOrWhiteSpace(req?.Actor) ? doc.Creator : req.Actor.Trim();
    var now = DateTime.UtcNow;

    doc.Status = DocumentStatuses.Submitted;
    doc.RejectionReason = null;
    doc.UpdatedAtUtc = now;

    var history = new ApprovalHistory
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        Action = "Submitted",
        FromStatus = oldStatus,
        ToStatus = DocumentStatuses.Submitted,
        PerformedBy = actor,
        Comment = string.IsNullOrWhiteSpace(req?.Comment) ? "Gửi tài liệu chờ phê duyệt" : req.Comment.Trim(),
        CorrelationId = correlationId,
        TimestampUtc = now
    };

    db.ApprovalHistories.Add(history);
    await db.SaveChangesAsync();

    var evt = new DocumentStatusChangedEvent(
        EventId: Guid.NewGuid(),
        DocumentId: doc.Id,
        Title: doc.Title,
        Category: doc.Category,
        Creator: doc.Creator,
        Approver: doc.Approver,
        OldStatus: oldStatus,
        NewStatus: DocumentStatuses.Submitted,
        ActionBy: actor,
        Comment: history.Comment,
        CorrelationId: correlationId,
        OccurredAtUtc: now
    );

    var published = await publisher.PublishDocumentStatusChangedAsync(evt);

    logger.LogInformation(
        "[CorrelationId={CorrelationId}] Tài liệu {DocumentId} đã chuyển sang Submitted (EventPublished={Published})",
        correlationId, doc.Id, published);

    doc.Histories = doc.Histories.OrderBy(h => h.TimestampUtc).ToList();
    return Results.Ok(new { document = doc, eventPublished = published, correlationId });
});

// 5. Phê duyệt tài liệu (Submitted -> Approved)
app.MapPost("/documents/{id:guid}/approve", async (
    Guid id,
    ApproveDocumentRequest? req,
    AppDbContext db,
    IEventPublisher publisher,
    HttpContext httpContext,
    ILogger<Program> logger) =>
{
    var doc = await db.Documents.Include(d => d.Histories).FirstOrDefaultAsync(d => d.Id == id);
    if (doc is null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy tài liệu có ID = {id}" });
    }

    if (doc.Status != DocumentStatuses.Submitted)
    {
        return Results.BadRequest(new
        {
            message = $"Chỉ có thể phê duyệt tài liệu đang ở trạng thái Submitted. Trạng thái hiện tại: {doc.Status}."
        });
    }

    var correlationId = httpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..12];
    var oldStatus = doc.Status;
    var actor = string.IsNullOrWhiteSpace(req?.Actor) ? doc.Approver : req.Actor.Trim();
    var now = DateTime.UtcNow;

    doc.Status = DocumentStatuses.Approved;
    doc.UpdatedAtUtc = now;

    var history = new ApprovalHistory
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        Action = "Approved",
        FromStatus = oldStatus,
        ToStatus = DocumentStatuses.Approved,
        PerformedBy = actor,
        Comment = string.IsNullOrWhiteSpace(req?.Comment) ? "Đã phê duyệt tài liệu" : req.Comment.Trim(),
        CorrelationId = correlationId,
        TimestampUtc = now
    };

    db.ApprovalHistories.Add(history);
    await db.SaveChangesAsync();

    var evt = new DocumentStatusChangedEvent(
        EventId: Guid.NewGuid(),
        DocumentId: doc.Id,
        Title: doc.Title,
        Category: doc.Category,
        Creator: doc.Creator,
        Approver: doc.Approver,
        OldStatus: oldStatus,
        NewStatus: DocumentStatuses.Approved,
        ActionBy: actor,
        Comment: history.Comment,
        CorrelationId: correlationId,
        OccurredAtUtc: now
    );

    var published = await publisher.PublishDocumentStatusChangedAsync(evt);

    logger.LogInformation(
        "[CorrelationId={CorrelationId}] Tài liệu {DocumentId} đã được Approved bởi {Actor}",
        correlationId, doc.Id, actor);

    doc.Histories = doc.Histories.OrderBy(h => h.TimestampUtc).ToList();
    return Results.Ok(new { document = doc, eventPublished = published, correlationId });
});

// 6. Từ chối tài liệu (Submitted -> Rejected)
app.MapPost("/documents/{id:guid}/reject", async (
    Guid id,
    RejectDocumentRequest? req,
    AppDbContext db,
    IEventPublisher publisher,
    HttpContext httpContext,
    ILogger<Program> logger) =>
{
    var doc = await db.Documents.Include(d => d.Histories).FirstOrDefaultAsync(d => d.Id == id);
    if (doc is null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy tài liệu có ID = {id}" });
    }

    if (doc.Status != DocumentStatuses.Submitted)
    {
        return Results.BadRequest(new
        {
            message = $"Chỉ có thể từ chối tài liệu đang ở trạng thái Submitted. Trạng thái hiện tại: {doc.Status}."
        });
    }

    var correlationId = httpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..12];
    var oldStatus = doc.Status;
    var actor = string.IsNullOrWhiteSpace(req?.Actor) ? doc.Approver : req.Actor.Trim();
    var reason = string.IsNullOrWhiteSpace(req?.Reason) ? "Chưa đạt yêu cầu nội dung, đề nghị chỉnh sửa lại." : req.Reason.Trim();
    var now = DateTime.UtcNow;

    doc.Status = DocumentStatuses.Rejected;
    doc.RejectionReason = reason;
    doc.UpdatedAtUtc = now;

    var history = new ApprovalHistory
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        Action = "Rejected",
        FromStatus = oldStatus,
        ToStatus = DocumentStatuses.Rejected,
        PerformedBy = actor,
        Comment = reason,
        CorrelationId = correlationId,
        TimestampUtc = now
    };

    db.ApprovalHistories.Add(history);
    await db.SaveChangesAsync();

    var evt = new DocumentStatusChangedEvent(
        EventId: Guid.NewGuid(),
        DocumentId: doc.Id,
        Title: doc.Title,
        Category: doc.Category,
        Creator: doc.Creator,
        Approver: doc.Approver,
        OldStatus: oldStatus,
        NewStatus: DocumentStatuses.Rejected,
        ActionBy: actor,
        Comment: reason,
        CorrelationId: correlationId,
        OccurredAtUtc: now
    );

    var published = await publisher.PublishDocumentStatusChangedAsync(evt);

    logger.LogInformation(
        "[CorrelationId={CorrelationId}] Tài liệu {DocumentId} bị Rejected bởi {Actor}. Lý do: {Reason}",
        correlationId, doc.Id, actor, reason);

    doc.Histories = doc.Histories.OrderBy(h => h.TimestampUtc).ToList();
    return Results.Ok(new { document = doc, eventPublished = published, correlationId });
});

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (int attempt = 1; attempt <= 12; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();

            if (!await db.Documents.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var doc1Id = Guid.NewGuid();
                var doc2Id = Guid.NewGuid();

                db.Documents.AddRange(
                    new DocumentItem
                    {
                        Id = doc1Id,
                        Title = "Đề xuất triển khai hệ thống lưu trữ phân tán",
                        Content = "Tài liệu trình bày phương án kiến trúc microservices, message broker RabbitMQ và cơ sở dữ liệu PostgreSQL.",
                        Category = "Đề xuất kỹ thuật",
                        Creator = "Lường Thị Giang",
                        Approver = "TS. Nguyễn Văn Trưởng Khoa",
                        Status = DocumentStatuses.Draft,
                        CreatedAtUtc = now.AddMinutes(-30),
                        UpdatedAtUtc = now.AddMinutes(-30),
                        Histories = new List<ApprovalHistory>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(),
                                DocumentId = doc1Id,
                                Action = "Created",
                                FromStatus = "None",
                                ToStatus = DocumentStatuses.Draft,
                                PerformedBy = "Lường Thị Giang",
                                Comment = "Khởi tạo hồ sơ mẫu",
                                CorrelationId = "seed-init-001",
                                TimestampUtc = now.AddMinutes(-30)
                            }
                        }
                    },
                    new DocumentItem
                    {
                        Id = doc2Id,
                        Title = "Kế hoạch nghiệm thu bài tập lớn CNTTK23E",
                        Content = "Danh sách các hạng mục kiểm thử tích hợp Docker Compose và kịch bản chịu lỗi mạng.",
                        Category = "Kế hoạch",
                        Creator = "Lường Thị Giang",
                        Approver = "Giảng viên hướng dẫn",
                        Status = DocumentStatuses.Submitted,
                        CreatedAtUtc = now.AddMinutes(-20),
                        UpdatedAtUtc = now.AddMinutes(-10),
                        Histories = new List<ApprovalHistory>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(),
                                DocumentId = doc2Id,
                                Action = "Created",
                                FromStatus = "None",
                                ToStatus = DocumentStatuses.Draft,
                                PerformedBy = "Lường Thị Giang",
                                Comment = "Tạo tài liệu kế hoạch",
                                CorrelationId = "seed-init-002",
                                TimestampUtc = now.AddMinutes(-20)
                            },
                            new()
                            {
                                Id = Guid.NewGuid(),
                                DocumentId = doc2Id,
                                Action = "Submitted",
                                FromStatus = DocumentStatuses.Draft,
                                ToStatus = DocumentStatuses.Submitted,
                                PerformedBy = "Lường Thị Giang",
                                Comment = "Gửi giảng viên phê duyệt",
                                CorrelationId = "seed-init-003",
                                TimestampUtc = now.AddMinutes(-10)
                            }
                        }
                    }
                );

                await db.SaveChangesAsync();
                logger.LogInformation("Đã khởi tạo dữ liệu mẫu thành công.");
            }

            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Đang chờ PostgreSQL sẵn sàng (lần thử {Attempt}/12)...", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
