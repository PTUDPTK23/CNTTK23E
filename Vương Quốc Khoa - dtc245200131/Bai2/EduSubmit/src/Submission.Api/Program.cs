// EduSubmit – Submission.Api (Máy chủ Minimal API)
// Học phần: Phát triển ứng dụng phân tán – Bài 2: RESTful API
// Cổng lắng nghe mặc định: http://localhost:5000

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ (In-memory storage) – Tạm thời để bài 2 có IsOpen = false để kiểm thử ca 400 ASSIGNMENT_CLOSED
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", true)
};

var submissions = new List<SubmissionDto>();

// Endpoint gốc: Kiểm tra dịch vụ
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// Endpoint 1: Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Endpoint 2: Xem chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Endpoint 3: Nộp bài tập (Tài nguyên con: /api/assignments/{assignmentId}/submissions)
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request) =>
{
    // 1. Kiểm tra bài tập có tồn tại không
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    }

    // 2. Kiểm tra bài tập có đang mở không
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));
    }

    // 3. Kiểm tra dữ liệu đầu vào
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));
    }

    // 4. Tạo và lưu lượt nộp
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow
    );

    submissions.Add(submission);

    // Trả 201 Created kèm header Location trỏ tới /api/submissions/{id}
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Endpoint 4: Đọc lại lượt nộp theo Guid
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

// ── KHAI BÁO DTO VÀ CONTRACT DỮ LIỆU CỦA SERVER ──────────────────────────
public record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
public record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
public record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
public record ApiError(string Code, string Message);
