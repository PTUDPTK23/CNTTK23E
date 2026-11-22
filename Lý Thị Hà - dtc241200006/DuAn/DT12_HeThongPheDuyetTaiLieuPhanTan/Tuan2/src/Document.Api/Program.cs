using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ SignalR vào DI (Phải đặt trước builder.Build())
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu lưu tạm trong RAM, bổ sung trường UpdatedAt
var documents = new List<DocumentDto>
{
    new(1, "Bao cao tai chinh Q3", "Chi phi va doanh thu quy 3", "Nguyen Van A", "Draft", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
    new(2, "Ke hoach trien khai du an", "Cac buoc ky thuat he thong", "Tran Thi B", "Submitted", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
};

var documentsLock = new object(); // Khóa an toàn khi đa tiến trình/tác vụ nền cùng thao tác

// 1. Kiểm tra trạng thái máy chủ
app.MapGet("/", () => Results.Ok(new { service = "Document.Api", status = "running" }));

// 2. Lấy danh sách toàn bộ tài liệu
app.MapGet("/documents", () => {
    lock (documentsLock) { return Results.Ok(documents); }
});

// 3. Xem chi tiết một tài liệu theo ID
app.MapGet("/documents/{id:int}", (int id) => {
    lock (documentsLock)
    {
        var doc = documents.FirstOrDefault(d => d.Id == id);
        return doc is null
            ? Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", "Khong tim thay tai lieu."))
            : Results.Ok(doc);
    }
});

// 4. Tạo tài liệu mới (Mặc định trạng thái là Draft)
app.MapPost("/documents", (CreateDocumentRequest request) => {
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Creator))
    {
        return Results.BadRequest(new ApiError("INVALID_DOCUMENT", "Tieu de va nguoi tao khong duoc de trong."));
    }

    DocumentDto newDoc;
    lock (documentsLock)
    {
        newDoc = new DocumentDto(
            Id: documents.Any() ? documents.Max(d => d.Id) + 1 : 1,
            Title: request.Title.Trim(),
            Description: request.Description?.Trim() ?? "",
            Creator: request.Creator.Trim(),
            Status: "Draft",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow
        );
        documents.Add(newDoc);
    }
    return Results.Created($"/documents/{newDoc.Id}", newDoc);
});

// 5. Gửi tài liệu đi phê duyệt (Draft -> Submitted)
// Tích hợp thêm IHubContext để mô phỏng quy trình xử lý nền (processing -> Approved) tự động đẩy tin real-time
app.MapPost("/documents/{id:int}/submit", (int id, IHubContext<DocumentHub> hubContext) => {
    DocumentDto updatedDoc;
    lock (documentsLock)
    {
        var doc = documents.FirstOrDefault(d => d.Id == id);
        if (doc is null) return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", "Khong tim thay tai lieu."));

        if (doc.Status != "Draft")
            return Results.BadRequest(new ApiError("INVALID_STATUS", "Chi co the gui phe duyet tai lieu o trang thai Draft."));

        updatedDoc = doc with { Status = "Submitted", UpdatedAt = DateTimeOffset.UtcNow };
        var index = documents.FindIndex(d => d.Id == id);
        documents[index] = updatedDoc;
    }

    // Khởi động tác vụ nền giả lập xử lý/phê duyệt ngầm, KHÔNG dùng await để trả về 201/200 ngay lập tức[cite: 129]
    _ = SimulateApprovalWorkflowAsync(id, documents, documentsLock, hubContext);

    return Results.Ok(updatedDoc);
});

// 6. Phê duyệt trực tiếp (Submitted -> Approved)
app.MapPost("/documents/{id:int}/approve", (int id, IHubContext<DocumentHub> hubContext) => {
    DocumentDto updatedDoc;
    lock (documentsLock)
    {
        var doc = documents.FirstOrDefault(d => d.Id == id);
        if (doc is null) return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", "Khong tim thay tai lieu."));
        if (doc.Status != "Submitted")
            return Results.BadRequest(new ApiError("INVALID_STATUS", "Chi co the phe duyet tai lieu dang o trang thai Submitted."));

        updatedDoc = doc with { Status = "Approved", UpdatedAt = DateTimeOffset.UtcNow };
        var index = documents.FindIndex(d => d.Id == id);
        documents[index] = updatedDoc;
    }

    // Đẩy thông báo tức thời qua SignalR tới nhóm của tài liệu
    var msg = new DocumentStatusMessage(updatedDoc.Id, updatedDoc.Status, updatedDoc.UpdatedAt);
    _ = hubContext.Clients.Group(DocumentGroups.ForDocument(id)).SendAsync("DocumentStatusChanged", msg);

    return Results.Ok(updatedDoc);
});

// 7. Từ chối trực tiếp (Submitted -> Rejected)
app.MapPost("/documents/{id:int}/reject", (int id, IHubContext<DocumentHub> hubContext) => {
    DocumentDto updatedDoc;
    lock (documentsLock)
    {
        var doc = documents.FirstOrDefault(d => d.Id == id);
        if (doc is null) return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", "Khong tim thay tai lieu."));
        if (doc.Status != "Submitted")
            return Results.BadRequest(new ApiError("INVALID_STATUS", "Chi co the tu choi tai lieu dang o trang thai Submitted."));

        updatedDoc = doc with { Status = "Rejected", UpdatedAt = DateTimeOffset.UtcNow };
        var index = documents.FindIndex(d => d.Id == id);
        documents[index] = updatedDoc;
    }

    var msg = new DocumentStatusMessage(updatedDoc.Id, updatedDoc.Status, updatedDoc.UpdatedAt);
    _ = hubContext.Clients.Group(DocumentGroups.ForDocument(id)).SendAsync("DocumentStatusChanged", msg);

    return Results.Ok(updatedDoc);
});

// Ánh xạ Hub SignalR vào đường dẫn tuyến tuân thủ quy chuẩn[cite: 127]
app.MapHub<DocumentHub>("/hubs/documents");

app.Run();

// --- TÁC VỤ NỀN GIẢ LẬP QUY TRÌNH CHẤM/DUYỆT TỰ ĐỘNG ---
static async Task SimulateApprovalWorkflowAsync(int id, List<DocumentDto> list, object gate, IHubContext<DocumentHub> hub)
{
    // Sau 1 giây chuyển sang "processing"
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateAndPushStatusAsync(id, "processing", list, gate, hub);

    // Sau thêm 2 giây chuyển sang "Approved" (hoặc từ chối tùy logic)
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateAndPushStatusAsync(id, "Approved", list, gate, hub);
}

static async Task UpdateAndPushStatusAsync(int id, string status, List<DocumentDto> list, object gate, IHubContext<DocumentHub> hub)
{
    DocumentDto? updated = null;
    lock (gate)
    {
        var i = list.FindIndex(d => d.Id == id);
        if (i >= 0)
        {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }

    if (updated is null) return;

    var msg = new DocumentStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    // Sử dụng IHubContext để phát thông điệp ra ngoài Hub tới đúng nhóm theo dõi tài liệu[cite: 127, 129]
    await hub.Clients.Group(DocumentGroups.ForDocument(id)).SendAsync("DocumentStatusChanged", msg);
}

// --- SIGNALR HUB VÀ QUẢN LÝ NHÓM ---
sealed class DocumentHub : Hub
{
    // Máy khách gọi phương thức này để gia nhập phòng/nhóm riêng theo ID tài liệu[cite: 127]
    public Task WatchDocument(string documentId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, DocumentGroups.ForDocument(documentId));
    }
}

static class DocumentGroups
{
    public static string ForDocument(int id) => ForDocument(id.ToString());
    public static string ForDocument(string id) => $"document:{id}";
}

// --- CÁC DTO VÀ RECORD HỢP ĐỒNG ---
record DocumentDto(int Id, string Title, string Description, string Creator, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
record CreateDocumentRequest(string Title, string Description, string Creator);
record DocumentStatusMessage(int DocumentId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);