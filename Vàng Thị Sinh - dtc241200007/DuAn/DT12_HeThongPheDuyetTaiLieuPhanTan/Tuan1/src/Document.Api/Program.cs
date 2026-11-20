var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// =========================================================================
// VÙNG 2: DỮ LIỆU MẪU TRONG BỘ NHỚ (In-Memory Storage cho Bài 2)
// =========================================================================
var documents = new List<DocumentRecord>
{
    new(
        Id: 1,
        Title: "De xuat ngan sach quy 4",
        Content: "Chi tiet du toan mua sam thiet bi phong lab.",
        CreatedBy: "SV001",
        AssignedTo: "GV01",
        Status: "Draft",
        CreatedAt: DateTimeOffset.UtcNow.AddHours(-2),
        History:
        [
            new ApprovalHistoryDto("CREATED", "SV001", "Tao ban nhap tai lieu", DateTimeOffset.UtcNow.AddHours(-2))
        ]
    ),
    new(
        Id: 2,
        Title: "Bao cao thuc tap chuyen nganh",
        Content: "Bao cao tong ket dot thuc tap he.",
        CreatedBy: "SV002",
        AssignedTo: "GV01",
        Status: "Submitted",
        CreatedAt: DateTimeOffset.UtcNow.AddHours(-5),
        History:
        [
            new ApprovalHistoryDto("CREATED", "SV002", "Tao tai lieu", DateTimeOffset.UtcNow.AddHours(-5)),
            new ApprovalHistoryDto("SUBMITTED", "SV002", "Gui phe duyet", DateTimeOffset.UtcNow.AddHours(-4))
        ]
    )
};

// =========================================================================
// VÙNG 3: CÁC ENDPOINT RESTFUL API
// =========================================================================

// 0. Health check cơ bản
app.MapGet("/", () => Results.Ok(new
{
    service = "Document.Api",
    status = "running"
}));

// 1. POST /api/documents : Tạo tài liệu mới (trạng thái khởi tạo là Draft)
app.MapPost("/api/documents", (CreateDocumentRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) ||
        string.IsNullOrWhiteSpace(request.Content) ||
        string.IsNullOrWhiteSpace(request.CreatedBy))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_DOCUMENT",
            "Tieu de, noi dung va nguoi tao khong duoc de trong."));
    }

    var newId = documents.Count == 0 ? 1 : documents.Max(d => d.Id) + 1;
    var now = DateTimeOffset.UtcNow;
    var creator = request.CreatedBy.Trim();
    var assignee = string.IsNullOrWhiteSpace(request.AssignedTo) ? "GV01" : request.AssignedTo.Trim();

    var doc = new DocumentRecord(
        Id: newId,
        Title: request.Title.Trim(),
        Content: request.Content.Trim(),
        CreatedBy: creator,
        AssignedTo: assignee,
        Status: "Draft",
        CreatedAt: now,
        History:
        [
            new ApprovalHistoryDto("CREATED", creator, "Tao moi tai lieu o trang thai Draft", now)
        ]
    );

    documents.Add(doc);
    return Results.Created($"/api/documents/{doc.Id}", ToDto(doc));
});

// 2. GET /api/documents : Danh sách tài liệu + Lọc theo status hoặc assignee (người xử lý)
app.MapGet("/api/documents", (string? status, string? assignee) =>
{
    IEnumerable<DocumentRecord> query = documents;

    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(d => d.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    if (!string.IsNullOrWhiteSpace(assignee))
    {
        query = query.Where(d => d.AssignedTo.Equals(assignee.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    return Results.Ok(query.Select(ToDto).ToList());
});

// 3. GET /api/documents/{id:int} : Xem chi tiết một tài liệu và lịch sử xử lý
app.MapGet("/api/documents/{id:int}", (int id) =>
{
    var doc = documents.FirstOrDefault(d => d.Id == id);
    return doc is null
        ? Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", $"Khong tim thay tai lieu co ma {id}."))
        : Results.Ok(ToDto(doc));
});

// 4. POST /api/documents/{id:int}/submit : Gửi tài liệu vào quy trình phê duyệt
app.MapPost("/api/documents/{id:int}/submit", (int id, SubmitDocumentRequest? request) =>
{
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null)
        return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", $"Khong tim thay tai lieu co ma {id}."));

    // Chỉ tài liệu ở trạng thái Draft mới được phép Submit
    if (!doc.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
        return Results.Conflict(new ApiError(
            "INVALID_STATE_TRANSITION",
            $"Khong the gui duyet tai lieu dang o trang thai '{doc.Status}'. Chi chap nhan 'Draft'."));

    var actor = string.IsNullOrWhiteSpace(request?.Actor) ? doc.CreatedBy : request.Actor.Trim();
    var note = string.IsNullOrWhiteSpace(request?.Note) ? "Gui tai lieu vao quy trinh phe duyet" : request.Note.Trim();

    doc.Status = "Submitted";
    doc.History.Add(new ApprovalHistoryDto("SUBMITTED", actor, note, DateTimeOffset.UtcNow));

    return Results.Ok(ToDto(doc));
});

// 5. POST /api/documents/{id:int}/approve : Phê duyệt tài liệu
app.MapPost("/api/documents/{id:int}/approve", (int id, ReviewDocumentRequest request) =>
{
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null)
        return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", $"Khong tim thay tai lieu co ma {id}."));

    if (string.IsNullOrWhiteSpace(request.Reviewer))
        return Results.BadRequest(new ApiError("INVALID_ACTION", "Thieu thong tin nguoi phe duyet (Reviewer)."));

    // Chỉ tài liệu đang Submitted mới được phép Approve
    if (!doc.Status.Equals("Submitted", StringComparison.OrdinalIgnoreCase))
        return Results.Conflict(new ApiError(
            "INVALID_STATE_TRANSITION",
            $"Khong the phe duyet tai lieu dang o trang thai '{doc.Status}'. Chi chap nhan 'Submitted'."));

    var reviewer = request.Reviewer.Trim();
    var note = string.IsNullOrWhiteSpace(request.Note) ? "Da phe duyet tai lieu" : request.Note.Trim();

    doc.Status = "Approved";
    doc.AssignedTo = reviewer;
    doc.History.Add(new ApprovalHistoryDto("APPROVED", reviewer, note, DateTimeOffset.UtcNow));

    return Results.Ok(ToDto(doc));
});

// 6. POST /api/documents/{id:int}/reject : Từ chối tài liệu
app.MapPost("/api/documents/{id:int}/reject", (int id, ReviewDocumentRequest request) =>
{
    var doc = documents.FirstOrDefault(d => d.Id == id);
    if (doc is null)
        return Results.NotFound(new ApiError("DOCUMENT_NOT_FOUND", $"Khong tim thay tai lieu co ma {id}."));

    if (string.IsNullOrWhiteSpace(request.Reviewer) || string.IsNullOrWhiteSpace(request.Note))
        return Results.BadRequest(new ApiError(
            "INVALID_ACTION",
            "Khi tu choi bat buoc phai co nguoi xu ly (Reviewer) va ly do tu choi (Note)."));

    // Chỉ tài liệu đang Submitted mới được phép Reject
    if (!doc.Status.Equals("Submitted", StringComparison.OrdinalIgnoreCase))
        return Results.Conflict(new ApiError(
            "INVALID_STATE_TRANSITION",
            $"Khong the tu choi tai lieu dang o trang thai '{doc.Status}'. Chi chap nhan 'Submitted'."));

    var reviewer = request.Reviewer.Trim();

    doc.Status = "Rejected";
    doc.AssignedTo = reviewer;
    doc.History.Add(new ApprovalHistoryDto("REJECTED", reviewer, request.Note.Trim(), DateTimeOffset.UtcNow));

    return Results.Ok(ToDto(doc));
});

// =========================================================================
// VÙNG 4: KHỞI CHẠY SERVER
// =========================================================================
app.Run();

// Hàm ánh xạ Entity nội bộ sang DTO đầu ra
static DocumentDto ToDto(DocumentRecord d) => new(
    d.Id, d.Title, d.Content, d.CreatedBy, d.AssignedTo, d.Status, d.CreatedAt, d.History);

// =========================================================================
// VÙNG 5: KHAI BÁO KIỂU DỮ LIỆU (DTO & ENTITY)
// =========================================================================
record CreateDocumentRequest(string Title, string Content, string CreatedBy, string? AssignedTo);
record SubmitDocumentRequest(string? Actor, string? Note);
record ReviewDocumentRequest(string Reviewer, string? Note);

record ApprovalHistoryDto(string Action, string Actor, string Note, DateTimeOffset Timestamp);

record DocumentDto(
    int Id,
    string Title,
    string Content,
    string CreatedBy,
    string AssignedTo,
    string Status,
    DateTimeOffset CreatedAt,
    List<ApprovalHistoryDto> History);

record ApiError(string Code, string Message);

// Mô hình lưu trữ nội bộ
class DocumentRecord(
    int Id,
    string Title,
    string Content,
    string CreatedBy,
    string AssignedTo,
    string Status,
    DateTimeOffset CreatedAt,
    List<ApprovalHistoryDto> History)
{
    public int Id { get; set; } = Id;
    public string Title { get; set; } = Title;
    public string Content { get; set; } = Content;
    public string CreatedBy { get; set; } = CreatedBy;
    public string AssignedTo { get; set; } = AssignedTo;
    public string Status { get; set; } = Status;
    public DateTimeOffset CreatedAt { get; set; } = CreatedAt;
    public List<ApprovalHistoryDto> History { get; set; } = History;
}
