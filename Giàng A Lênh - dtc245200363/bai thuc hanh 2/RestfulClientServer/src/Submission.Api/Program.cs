using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var app = builder.Build();

var assignments = new ConcurrentDictionary<int, AssignmentDto>(new[]
{
    new KeyValuePair<int, AssignmentDto>(1, new(1, "Bai tap REST API", "Thiet ke endpoint RESTful va hop dong HTTP.", true)),
    new KeyValuePair<int, AssignmentDto>(2, new(2, "Bai tap Client", "Viet console client goi Submission API.", false))
});
var submissions = new ConcurrentDictionary<Guid, SubmissionDto>();
var submissionMutationLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

app.MapGet("/api/assignments", () =>
    Results.Ok(assignments.Values.OrderBy(assignment => assignment.Id)));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    return assignments.TryGetValue(assignmentId, out var assignment)
        ? Results.Ok(assignment)
        : Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request) =>
{
    if (!assignments.TryGetValue(assignmentId, out var assignment))
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop bai."));
    }

    var studentCode = request.StudentCode?.Trim();
    var repositoryUrl = request.RepositoryUrl?.Trim();
    if (string.IsNullOrWhiteSpace(studentCode) || string.IsNullOrWhiteSpace(repositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma sinh vien va repository URL khong duoc de trong."));
    }

    SubmissionDto submission;
    lock (submissionMutationLock)
    {
        var duplicate = submissions.Values.Any(item =>
            item.AssignmentId == assignmentId &&
            string.Equals(item.StudentCode, studentCode, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));
        }

        submission = new SubmissionDto(
            Guid.NewGuid(), assignmentId, studentCode, repositoryUrl, "received", DateTimeOffset.UtcNow);
        submissions[submission.Id] = submission;
    }

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    return submissions.TryGetValue(submissionId, out var submission)
        ? Results.Ok(submission)
        : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));
});

// Bước mở rộng 9: xóa lượt nộp, trả 204 khi xóa thành công.
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionMutationLock)
    {
        return submissions.TryRemove(submissionId, out _)
            ? Results.NoContent()
            : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));
    }
});

app.Run();

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
