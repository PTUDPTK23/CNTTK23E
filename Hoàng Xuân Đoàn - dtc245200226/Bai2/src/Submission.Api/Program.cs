var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu bài tập trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint cho he thong", true),
    new(2, "Bai tap Client", "Viet console client goi API", true),
    new(3, "Bai tap Nang Cao", "Bai nay da dong", false)
};

// Danh sách lượt nộp bài của sinh viên
var submissions = new List<SubmissionDto>();

// Kiểm tra trạng thái API
app.MapGet("/", () => new { service = "Submission.Api", status = "running" });

// 1. GET danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 2. GET chi tiết một bài tập theo ID
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// 3. POST nộp bài (Tài nguyên con thuộc bài tập)[cite: 66, 79]
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request) =>
{
    // Cổng 1: Kiểm tra bài tập có tồn tại không
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    // Cổng 2: Kiểm tra bài tập còn mở không
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap nay da dong, khong nhan nop nua."));

    // Cổng 3: Kiểm tra dữ liệu đầu vào hợp lệ
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac duong dan kho ma nguon."));

    // Tạo lượt nộp mới
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow
    );

    submissions.Add(submission);

    // Trả về 201 Created kèm header Location và đối tượng ở Body[cite: 51, 79]
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. GET đọc lại lượt nộp theo ID (Guid)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

// Khai báo các Record DTO ở cuối tệp[cite: 43, 44]
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);