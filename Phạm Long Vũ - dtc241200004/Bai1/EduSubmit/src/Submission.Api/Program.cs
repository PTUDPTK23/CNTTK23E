var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Danh sách bài tập mẫu
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Goi API tu client")
};

// Lưu trữ các lượt nộp bài trong bộ nhớ (Bước 12)
var submissions = new List<SubmissionResult>();

// GET / : kiểm tra dịch vụ đang chạy
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// GET /api/assignments : lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// POST /api/submissions : tạo lượt nộp bài mới
app.MapPost("/api/submissions", (CreateSubmissionRequest request) =>
{
    // Kiểm tra tính hợp lệ của dữ liệu đầu vào
    if (request.AssignmentId <= 0 ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new { message = "Du lieu nop bai khong hop le." });
    }

    // Tạo bản ghi lượt nộp bài
    var submission = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    // Lưu vào danh sách (Bước 12)
    submissions.Add(submission);

    // Trả về 201 Created kèm header Location
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{id:guid} : đọc chi tiết một lượt nộp bài theo id (Bước 12)
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    var submission = submissions.FirstOrDefault(s => s.Id == id);
    return submission is not null
        ? Results.Ok(submission)
        : Results.NotFound(new { message = $"Khong tim thay luot nop voi ma: {id}" });
});

app.Run();

// Hợp đồng dữ liệu
record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt);
