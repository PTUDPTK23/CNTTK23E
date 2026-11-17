var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu bài tập trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", true),
    new(3, "Bai tap Cuoi ky (Da dong)", "Da het han nop...", false)
};

// Danh sách lưu trữ các lượt nộp bài
var submissions = new List<SubmissionDto>();

// GET / : kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// GET /api/assignments : danh sách tất cả bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// POST /api/assignments/{assignmentId:int}/submissions : nộp bài cho bài tập
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    // Cổng 1: Kiểm tra bài tập có tồn tại không?
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    // Cổng 2: Kiểm tra bài tập còn mở nhận bài không?
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop nua."));

    // Cổng 3: Kiểm tra tính hợp lệ của dữ liệu đầu vào (Biên hệ thống)
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon repo."));

    // Cổng 4 (Mở rộng Bước 9): Kiểm tra trùng lặp sinh viên đã nộp bài này chưa
    var duplicated = submissions.Any(item =>
        item.AssignmentId == assignmentId &&
        item.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase));
    if (duplicated)
        return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay roi."));

    // Tạo đối tượng SubmissionDto (Server tự sinh Id, Status, ReceivedAt)
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow);

    submissions.Add(submission);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{submissionId:guid} : đọc lại lượt nộp theo Id (Guid)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

// DELETE /api/submissions/{submissionId:guid} : xóa/rút lượt nộp (Bước 9 mở rộng)
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var s = submissions.FirstOrDefault(i => i.Id == submissionId);
    if (s is null)
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop de xoa."));

    submissions.Remove(s);
    return Results.NoContent();
});

app.Run();

// DTO & ApiError ở cuối tệp
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);

