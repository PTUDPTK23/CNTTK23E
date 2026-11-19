var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu lưu tạm trong bộ nhớ (RAM) cho hệ thống tài liệu
var documents = new List<DocumentDto>
{
    new(1, "Bao cao tai chinh Q3", "Chi phi va doanh thu quy 3", "Nguyen Van A", "Draft", DateTimeOffset.UtcNow),
    new(2, "Ke hoach trien khai du an", "Cac buoc ky thuat he thong", "Tran Thi B", "Submitted", DateTimeOffset.UtcNow)
};

// 1. GET / : Kiểm tra trạng thái máy chủ
app.MapGet("/", () => Results.Ok(new { service = "Document.Api", status = "running" }));

// 2. GET /documents : Lấy danh sách toàn bộ tài liệu
app.MapGet("/documents", () => Results.Ok(documents));

// 3. GET /documents/{id} : Xem chi tiết một tài liệu theo ID
app.MapGet("/documents/{id:int}", (int id) => {
    var doc = documents.FirstOrDefault(d => d.Id == id);
    return doc is null
        ? Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", "Khong tim thay tai lieu."))
        : Results.Ok(doc);
});

// 4. POST /documents : Tạo tài liệu mới (Mặc định trạng thái là Draft)
app.MapPost("/documents", (CreateDocumentRequest request) => {
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Creator))
    {
        return Results.BadRequest(new ApiError("INVALID_DOCUMENT", "Tieu de va nguoi tao khong duoc de trong."));
    }

    var newDoc = new DocumentDto(
        Id: documents.Any() ? documents.Max(d => d.Id) + 1 : 1,
        Title: request.Title.Trim(),
        Description: request.Description?.Trim() ?? "",
        Creator: request.Creator.Trim(),
        Status: "Draft", // Trạng thái mặc định ban đầu theo đề tài
        CreatedAt: DateTimeOffset.UtcNow
    );

    documents.Add(newDoc);
    return Results.Created($"/documents/{newDoc.Id}", newDoc);
});

// 5. POST /documents/{id}/submit : Gửi tài liệu vào quy trình phê duyệt (Draft -> Submitted)
app.MapPost("/documents/{id:int}/submit", (int id) => {
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null) return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", "Khong tim thay tai lieu."));

    if (doc.Status != "Draft")
        return Results.BadRequest(new ApiError("INVALID_STATUS", "Chi co the gui phe duyet tai lieu o trang thai Draft."));

    var updatedDoc = doc with { Status = "Submitted" };
    documents.Remove(doc);
    documents.Add(updatedDoc);

    return Results.Ok(updatedDoc);
});

// 6. POST /documents/{id}/approve : Phê duyệt tài liệu (Submitted -> Approved)
app.MapPost("/documents/{id:int}/approve", (int id) => {
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null) return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", "Khong tim thay tai lieu."));

    if (doc.Status != "Submitted")
        return Results.BadRequest(new ApiError("INVALID_STATUS", "Chi co the phe duyet tai lieu dang o trang thai Submitted."));

    var updatedDoc = doc with { Status = "Approved" };
    documents.Remove(doc);
    documents.Add(updatedDoc);

    return Results.Ok(updatedDoc);
});

// 7. POST /documents/{id}/reject : Từ chối tài liệu (Submitted -> Rejected)
app.MapPost("/documents/{id:int}/reject", (int id) => {
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null) return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", "Khong tim thay tai lieu."));

    if (doc.Status != "Submitted")
        return Results.BadRequest(new ApiError("INVALID_STATUS", "Chi co the tu choi tai lieu dang o trang thai Submitted."));

    var updatedDoc = doc with { Status = "Rejected" };
    documents.Remove(doc);
    documents.Add(updatedDoc);

    return Results.Ok(updatedDoc);
});

app.Run();

// Khai báo DTO và mô hình lỗi chuẩn (ApiError)
record DocumentDto(int Id, string Title, string Description, string Creator, string Status, DateTimeOffset CreatedAt);
record CreateDocumentRequest(string Title, string Description, string Creator);
record ApiError(string Code, string Message);