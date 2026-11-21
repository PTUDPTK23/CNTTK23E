using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

var documents = new List<DocumentDto>();
var documentsLock = new object();

// Endpoint kiểm tra sức khỏe dịch vụ
app.MapGet("/", () => Results.Ok(new
{
    service = "DocumentApprovalApi",
    week = "Tuan 2 - REST + SignalR",
    status = "running"
}));

// REST: Lấy danh sách tài liệu
app.MapGet("/api/documents", () =>
{
    lock (documentsLock)
    {
        return Results.Ok(documents);
    }
});

// REST: Lấy chi tiết một tài liệu (Nguồn sự thật khi mất kết nối)
app.MapGet("/api/documents/{documentId:guid}", (Guid documentId) =>
{
    lock (documentsLock)
    {
        var doc = documents.FirstOrDefault(d => d.Id == documentId);
        return doc is null
            ? Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", $"Khong tim thay tai lieu {documentId}"))
            : Results.Ok(doc);
    }
});

// REST: Gửi yêu cầu phê duyệt tài liệu mới + kích hoạt luồng duyệt nền qua IHubContext
app.MapPost("/api/documents", (CreateDocumentRequest request, IHubContext<ApprovalHub> hubContext) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.SubmittedBy))
    {
        return Results.BadRequest(new ApiError("INVALID_INPUT", "Title va SubmittedBy khong duoc de trong."));
    }

    var now = DateTimeOffset.UtcNow;
    var doc = new DocumentDto(
        Id: Guid.NewGuid(),
        Title: request.Title.Trim(),
        SubmittedBy: request.SubmittedBy.Trim(),
        FileUrl: string.IsNullOrWhiteSpace(request.FileUrl) ? "https://example.com/default.pdf" : request.FileUrl.Trim(),
        Status: "submitted",
        CreatedAt: now,
        UpdatedAt: now
    );

    lock (documentsLock)
    {
        documents.Add(doc);
    }

    // Chạy luồng phê duyệt ở nền, KHÔNG await để trả 201 Created ngay lập tức
    _ = SimulateApprovalWorkflowAsync(doc.Id, documents, documentsLock, hubContext);

    return Results.Created($"/api/documents/{doc.Id}", doc);
});

// 2. Ánh xạ Hub SignalR
app.MapHub<ApprovalHub>("/hubs/approvals");

app.Run();

// Hàm giả lập quy trình phê duyệt phân tán ở nền
static async Task SimulateApprovalWorkflowAsync(
    Guid id,
    List<DocumentDto> list,
    object gate,
    IHubContext<ApprovalHub> hub)
{
    // Sau 1.5 giây: chuyển sang đang thẩm định (reviewing)
    await Task.Delay(TimeSpan.FromSeconds(1.5));
    await UpdateStatusAsync(id, "reviewing", "Dang kiem tra noi dung va chu ky so...", list, gate, hub);

    // Sau 2.5 giây tiếp theo: quyết định phê duyệt (approved) hoặc từ chối (rejected)
    await Task.Delay(TimeSpan.FromSeconds(2.5));
    var isApproved = true;
    lock (gate)
    {
        var current = list.FirstOrDefault(d => d.Id == id);
        if (current is not null && !current.FileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            isApproved = false;
        }
    }

    if (isApproved)
    {
        await UpdateStatusAsync(id, "approved", "Tai lieu da duoc phe duyet hop le.", list, gate, hub);
    }
    else
    {
        await UpdateStatusAsync(id, "rejected", "Tu choi: FileUrl phai bat dau bang https://", list, gate, hub);
    }
}

// Cập nhật dữ liệu trước, đẩy thông báo tối thiểu tới đúng nhóm sau
static async Task UpdateStatusAsync(
    Guid id,
    string status,
    string note,
    List<DocumentDto> list,
    object gate,
    IHubContext<ApprovalHub> hub)
{
    DocumentDto? updated = null;
    lock (gate)
    {
        var index = list.FindIndex(d => d.Id == id);
        if (index >= 0)
        {
            updated = list[index] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            list[index] = updated;
        }
    }

    if (updated is null) return;

    var msg = new DocumentStatusMessage(updated.Id, updated.Status, note, updated.UpdatedAt);
    Console.WriteLine($"[Push] {ApprovalGroups.ForDocument(id)} -> {status}");

    await hub.Clients
        .Group(ApprovalGroups.ForDocument(id))
        .SendAsync("DocumentStatusChanged", msg);
}

// Hub mỏng: chỉ quản lý việc đăng ký kết nối vào nhóm của từng tài liệu
sealed class ApprovalHub : Hub
{
    public async Task WatchDocument(string documentId)
    {
        var groupName = ApprovalGroups.ForDocument(documentId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
    }
}

static class ApprovalGroups
{
    public static string ForDocument(Guid id) => ForDocument(id.ToString());
    public static string ForDocument(string id) => $"document:{id}";
}

record CreateDocumentRequest(string Title, string SubmittedBy, string? FileUrl);
record DocumentDto(Guid Id, string Title, string SubmittedBy, string FileUrl, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
record DocumentStatusMessage(Guid DocumentId, string Status, string Note, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);