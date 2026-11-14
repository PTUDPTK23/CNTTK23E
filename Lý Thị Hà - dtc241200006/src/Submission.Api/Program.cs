var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
// khai báo DTO, dữ liệu mẫu và endpoint danh sách bài tập
var assignments = new List<AssignmentDto> {
new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
new(2, "Bai tap Client", "Viet console client...", true)
};
// Danh sách lưu trữ lượt nộp trong bộ nhớ

var submissions = new List<SubmissionDto>();

// GET /api/assignments: Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// // GET /api/assignments/{assignmentId:int}: Lấy chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
return assignment is null
? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
: Results.Ok(assignment);
});
// POST /api/assignments/{assignmentId}/submissions: Nộp bài
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request) => {
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac repo."));
        var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow
    );
    submissions.Add(submission);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{submissionId}: Đọc lại thông tin lượt nộp
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();
// --- VÙNG KHAI BÁO DTO (BẮT BUỘC NẰM Ở CUỐI CÙNG CỦA TỆP) ---
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);