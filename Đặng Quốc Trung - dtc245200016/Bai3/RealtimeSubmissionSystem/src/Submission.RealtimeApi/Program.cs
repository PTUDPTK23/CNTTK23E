var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Danh sach bai tap
var assignments = new List<AssignmentDto>
{
    new(1, "Bai 3 - SignalR", true),
    new(2, "Bai tap da dong", false)
};

// Danh sach luot nop va khoa du lieu
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// Kiem tra may chu
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// Lay danh sach bai tap
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// Tao luot nop bai
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED",
            "Bai tap da dong."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Thieu ma sinh vien hoac URL repository."));
    }

    var now = DateTimeOffset.UtcNow;

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode,
        request.RepositoryUrl,
        "received",
        now,
        now
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    return Results.Created(
        $"/api/submissions/{submission.Id}", submission);
});

// Doc thong tin luot nop
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            s => s.Id == submissionId);

        return submission is null
            ? Results.NotFound(new ApiError(
                "SUBMISSION_NOT_FOUND",
                "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

app.Run();

// Cac DTO
record AssignmentDto(
    int Id,
    string Title,
    bool IsOpen);

record CreateSubmissionRequest(
    string? StudentCode,
    string? RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);
