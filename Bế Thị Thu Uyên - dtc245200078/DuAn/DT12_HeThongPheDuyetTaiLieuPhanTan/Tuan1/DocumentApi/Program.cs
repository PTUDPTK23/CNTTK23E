using DocumentApi.Data;
using DocumentApi.Models;
using DocumentApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<RabbitMqPublisher>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 1. POST /documents - Tạo tài liệu mới (Trạng thái mặc định: Draft)
app.MapPost("/documents", async (CreateDocumentRequest req, DocumentDbContext db, RabbitMqPublisher publisher, ILogger<Program> logger) =>
{
    var doc = new Document
    {
        Title = req.Title,
        Content = req.Content,
        CreatedBy = req.CreatedBy,
        Approver = req.Approver,
        Status = "Draft",
        CreatedAt = DateTime.UtcNow
    };

    db.Documents.Add(doc);
    await db.SaveChangesAsync();

    db.DocumentHistories.Add(new DocumentHistory
    {
        DocumentId = doc.Id,
        Action = "Created",
        Actor = doc.CreatedBy,
        Note = "Tạo tài liệu nháp"
    });
    await db.SaveChangesAsync();

    logger.LogInformation("[API] Đã tạo tài liệu #{Id} - '{Title}' bởi {User}", doc.Id, doc.Title, doc.CreatedBy);

    try
    {
        await publisher.PublishEventAsync(new { Event = "DocumentCreated", doc.Id, doc.Title, doc.Status, Actor = doc.CreatedBy, doc.Approver, Timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        logger.LogWarning("[RabbitMQ] Không thể gửi sự kiện DocumentCreated: {Message}", ex.Message);
    }

    return Results.Created($"/documents/{doc.Id}", doc);
});

// 2. GET /documents - Tra cứu danh sách (hỗ trợ lọc theo status và approver)
app.MapGet("/documents", async (string? status, string? approver, DocumentDbContext db) =>
{
    var query = db.Documents.AsQueryable();

    if (!string.IsNullOrWhiteSpace(status))
        query = query.Where(d => d.Status.ToLower() == status.ToLower());

    if (!string.IsNullOrWhiteSpace(approver))
        query = query.Where(d => d.Approver != null && d.Approver.ToLower() == approver.ToLower());

    var docs = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
    return Results.Ok(docs);
});

// 3. GET /documents/{id} - Xem chi tiết tài liệu kèm lịch sử xử lý
app.MapGet("/documents/{id:int}", async (int id, DocumentDbContext db) =>
{
    var doc = await db.Documents.FindAsync(id);
    if (doc is null) return Results.NotFound(new { Message = $"Không tìm thấy tài liệu #{id}" });

    var histories = await db.DocumentHistories
        .Where(h => h.DocumentId == id)
        .OrderBy(h => h.Timestamp)
        .ToListAsync();

    return Results.Ok(new { Document = doc, Histories = histories });
});

// 4. POST /documents/{id}/submit - Gửi tài liệu vào quy trình phê duyệt
app.MapPost("/documents/{id:int}/submit", async (int id, ActionRequest req, DocumentDbContext db, RabbitMqPublisher publisher, ILogger<Program> logger) =>
{
    var doc = await db.Documents.FindAsync(id);
    if (doc is null) return Results.NotFound(new { Message = $"Không tìm thấy tài liệu #{id}" });

    if (doc.Status != "Draft" && doc.Status != "Rejected")
        return Results.BadRequest(new { Message = $"Không thể gửi duyệt tài liệu đang ở trạng thái '{doc.Status}'" });

    doc.Status = "Submitted";
    if (!string.IsNullOrWhiteSpace(req.Approver)) doc.Approver = req.Approver;
    doc.UpdatedAt = DateTime.UtcNow;

    db.DocumentHistories.Add(new DocumentHistory
    {
        DocumentId = doc.Id,
        Action = "Submitted",
        Actor = req.Actor,
        Note = req.Note ?? "Gửi trình phê duyệt"
    });
    await db.SaveChangesAsync();

    logger.LogInformation("[API] Tài liệu #{Id} đã được gửi duyệt bởi {Actor} tới {Approver}", doc.Id, req.Actor, doc.Approver);

    try
    {
        await publisher.PublishEventAsync(new { Event = "DocumentSubmitted", doc.Id, doc.Title, doc.Status, Actor = req.Actor, doc.Approver, Timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        logger.LogWarning("[RabbitMQ] Không thể gửi sự kiện DocumentSubmitted: {Message}", ex.Message);
    }

    return Results.Ok(doc);
});

// 5. POST /documents/{id}/approve - Phê duyệt tài liệu
app.MapPost("/documents/{id:int}/approve", async (int id, ActionRequest req, DocumentDbContext db, RabbitMqPublisher publisher, ILogger<Program> logger) =>
{
    var doc = await db.Documents.FindAsync(id);
    if (doc is null) return Results.NotFound(new { Message = $"Không tìm thấy tài liệu #{id}" });

    if (doc.Status != "Submitted")
        return Results.BadRequest(new { Message = $"Chỉ có thể phê duyệt tài liệu ở trạng thái 'Submitted' (hiện tại: '{doc.Status}')" });

    doc.Status = "Approved";
    doc.Approver = req.Actor;
    doc.UpdatedAt = DateTime.UtcNow;

    db.DocumentHistories.Add(new DocumentHistory
    {
        DocumentId = doc.Id,
        Action = "Approved",
        Actor = req.Actor,
        Note = req.Note ?? "Đã phê duyệt"
    });
    await db.SaveChangesAsync();

    logger.LogInformation("[API] Tài liệu #{Id} đã được PHÊ DUYỆT bởi {Actor}", doc.Id, req.Actor);

    try
    {
        await publisher.PublishEventAsync(new { Event = "DocumentApproved", doc.Id, doc.Title, doc.Status, Actor = req.Actor, Creator = doc.CreatedBy, Timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        logger.LogWarning("[RabbitMQ] Không thể gửi sự kiện DocumentApproved: {Message}", ex.Message);
    }

    return Results.Ok(doc);
});

// 6. POST /documents/{id}/reject - Từ chối tài liệu
app.MapPost("/documents/{id:int}/reject", async (int id, ActionRequest req, DocumentDbContext db, RabbitMqPublisher publisher, ILogger<Program> logger) =>
{
    var doc = await db.Documents.FindAsync(id);
    if (doc is null) return Results.NotFound(new { Message = $"Không tìm thấy tài liệu #{id}" });

    if (doc.Status != "Submitted")
        return Results.BadRequest(new { Message = $"Chỉ có thể từ chối tài liệu ở trạng thái 'Submitted' (hiện tại: '{doc.Status}')" });

    doc.Status = "Rejected";
    doc.Approver = req.Actor;
    doc.UpdatedAt = DateTime.UtcNow;

    db.DocumentHistories.Add(new DocumentHistory
    {
        DocumentId = doc.Id,
        Action = "Rejected",
        Actor = req.Actor,
        Note = req.Note ?? "Từ chối phê duyệt"
    });
    await db.SaveChangesAsync();

    logger.LogInformation("[API] Tài liệu #{Id} đã bị TỪ CHỐI bởi {Actor}. Lý do: {Note}", doc.Id, req.Actor, req.Note);

    try
    {
        await publisher.PublishEventAsync(new { Event = "DocumentRejected", doc.Id, doc.Title, doc.Status, Actor = req.Actor, Creator = doc.CreatedBy, Note = req.Note, Timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        logger.LogWarning("[RabbitMQ] Không thể gửi sự kiện DocumentRejected: {Message}", ex.Message);
    }

    return Results.Ok(doc);
});
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    db.Database.EnsureCreated();
}
app.Run();

// Các DTO nhận dữ liệu đầu vào
record CreateDocumentRequest(string Title, string Content, string CreatedBy, string? Approver);
record ActionRequest(string Actor, string? Approver, string? Note);