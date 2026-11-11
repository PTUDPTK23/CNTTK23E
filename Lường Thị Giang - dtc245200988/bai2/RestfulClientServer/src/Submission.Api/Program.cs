var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", false), // bài 2 đã đóng
    new(3, "Bai tap HttpClient", "Thu nghiem Client", true)    // thêm bài tập 3
};

var submissions = new List<SubmissionDto>();

app.MapGet("/", () => "Hello World!");

app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Lấy chi tiết bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);

    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Nộp bài
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    // 1. Kiểm tra bài tập có tồn tại
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(
            new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    }

    // 2. Kiểm tra bài còn mở
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(
            new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
    }

    // 3. Kiểm tra dữ liệu
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(
            new ApiError("INVALID_SUBMISSION", "Thong tin nop bai khong hop le."));
    }

    // 4. Không cho nộp trùng
    bool alreadySubmitted = submissions.Any(s =>
        s.AssignmentId == assignmentId &&
        s.StudentCode.Equals(
            request.StudentCode.Trim(),
            StringComparison.OrdinalIgnoreCase));

    if (alreadySubmitted)
    {
        return Results.BadRequest(
            new ApiError(
                "ALREADY_SUBMITTED",
                "Sinh vien da nop bai tap nay."));
    }

    // 5. Tạo submission
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow);

    submissions.Add(submission);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// ===== BƯỚC 6: Đọc lại lượt nộp =====
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);

    return submission is null
        ? Results.NotFound(
            new ApiError(
                "SUBMISSION_NOT_FOUND",
                "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record ApiError(
    string Code,
    string Message);