var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Viet client goi API")
};
var submissions = new List<SubmissionResult>();   // Bước 12: lưu các lượt nộp

// GET / : kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

// GET /api/assignments : danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// POST /api/submissions : nộp bài
app.MapPost("/api/submissions", (CreateSubmissionRequest request) =>
{
    // 1. Máy chủ tự kiểm tra dữ liệu, không tin máy khách
    if (request.AssignmentId <= 0 ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new { message = "Thieu du lieu hoac assignmentId khong hop le." });
    }

    // 2. Máy chủ tự sinh Id, trạng thái và thời điểm nhận
    var submission = new SubmissionResult(Guid.NewGuid(), request.AssignmentId,
        request.StudentCode.Trim(), "received", DateTimeOffset.UtcNow);
    submissions.Add(submission);

    // 3. 201 Created + header Location trỏ tới địa chỉ đọc lại
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Bước 12 (mở rộng): GET /api/submissions/{id} : đọc lại một lượt nộp
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    var submission = submissions.FirstOrDefault(s => s.Id == id);
    return submission is null
        ? Results.NotFound(new { message = "Khong tim thay luot nop." })
        : Results.Ok(submission);
});

app.Run();

record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode,
    string Status, DateTimeOffset ReceivedAt);
