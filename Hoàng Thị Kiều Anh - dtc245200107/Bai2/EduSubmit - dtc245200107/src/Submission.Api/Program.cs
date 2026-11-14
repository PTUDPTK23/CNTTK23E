
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu mau
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", false),
    new(3, "Bai tap mo rong", "Bai tap da dong cho ca 9a.", false)
};

var submissions = new List<SubmissionDto>();

// GET / - Kiem tra may chu
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// GET - Danh sach bai tap
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// GET - Chi tiet bai tap
app.MapGet("/api/assignments/{assignmentId:int}",
    (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    return Results.Ok(assignment);
});

// POST - Nop bai
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    // Bai tap khong ton tai
    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    // Bai tap da dong
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED",
            "Bai tap da dong."));
    }

    // Kiem tra du lieu
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Ma sinh vien va link bai nop khong duoc de trong."));
    }

    // Kiem tra nop trung
    var existing = submissions.FirstOrDefault(
        s => s.AssignmentId == assignmentId &&
             s.StudentCode.Equals(
                 request.StudentCode.Trim(),
                 StringComparison.OrdinalIgnoreCase));

    if (existing is not null)
    {
        return Results.Conflict(new ApiError(
            "DUPLICATE_SUBMISSION",
            "Sinh vien da nop bai tap nay."));
    }

    // Tao bai nop moi
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow
    );

    submissions.Add(submission);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// GET - Doc lai bai nop
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(
        s => s.Id == submissionId);

    if (submission is null)
    {
        return Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay bai nop."));
    }

    return Results.Ok(submission);
});

// DELETE - Rut bai nop
app.MapDelete("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(
        s => s.Id == submissionId);

    if (submission is null)
    {
        return Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay bai nop."));
    }

    submissions.Remove(submission);

    return Results.NoContent();
});

app.Run();

// DTO
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
