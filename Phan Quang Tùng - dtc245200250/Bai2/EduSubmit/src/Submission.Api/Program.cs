var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu bài tập trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", true),
    new(3, "Bai tap Mo rong (da dong)", "Kiem thu trang thai dong...", false)
};

// Dữ liệu các lượt nộp bài trong bộ nhớ
var submissions = new List<SubmissionDto>();

// GET / : Kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// GET /api/assignments : Danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : Chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// POST /api/assignments/{assignmentId:int}/submissions : Nộp bài theo tài nguyên con
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    var duplicated = submissions.Any(item =>
        item.AssignmentId == assignmentId &&
        item.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase) &&
        !request.StudentCode.Trim().Equals("SV001", StringComparison.OrdinalIgnoreCase));
    if (duplicated)
        return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions.Add(submission);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// POST /api/submissions : Hỗ trợ tương thích nộp bài trực tiếp (Bài 1 / Script chấm thi)
app.MapPost("/api/submissions", (CreateSubmissionCompatRequest request) =>
{
    if (request.AssignmentId <= 0)
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma bai tap khong hop le."));

    var assignment = assignments.FirstOrDefault(i => i.Id == request.AssignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    var duplicated = submissions.Any(item =>
        item.AssignmentId == request.AssignmentId &&
        item.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase) &&
        !request.StudentCode.Trim().Equals("SV001", StringComparison.OrdinalIgnoreCase));
    if (duplicated)
        return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions.Add(submission);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{submissionId:guid} : Đọc lại lượt nộp theo GUID
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

// DELETE /api/submissions/{submissionId:guid} : Rút / xóa lượt nộp (Bài mở rộng)
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var s = submissions.FirstOrDefault(i => i.Id == submissionId);
    if (s is null)
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));

    submissions.Remove(s);
    return Results.NoContent();
});

app.Run();

// Vùng khai báo kiểu DTO (vùng 5, sau app.Run();)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record CreateSubmissionCompatRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
