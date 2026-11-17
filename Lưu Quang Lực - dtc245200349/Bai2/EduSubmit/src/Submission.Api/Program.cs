using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var assignments = new AssignmentDto[]
{
    new(1, "Bai tap REST API", "Thiet ke endpoint.", true),
    new(2, "Bai tap Client", "Viet console client.", true),
    new(3, "Bai tap da dong", "Dung de thu quy tac dong bai tap.", false)
};
var submissions = new ConcurrentDictionary<Guid, SubmissionDto>();

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));
    }

    var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
        request.StudentCode.Trim(), request.RepositoryUrl.Trim(), "received", DateTimeOffset.UtcNow);

    submissions[submission.Id] = submission;
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
    submissions.TryGetValue(submissionId, out var submission)
        ? Results.Ok(submission)
        : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop.")));

app.Run();

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
