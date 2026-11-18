using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", true),
    new(2, "REST va realtime", true),
    new(3, "Bai tap da dong", false)
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "EduSubmit Realtime API", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments.Where(a => a.IsOpen)));
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
    assignments.FirstOrDefault(a => a.Id == assignmentId) is { } found
        ? Results.Ok(found) : Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap.")));
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
    {
        var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
        if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Can studentCode va repositoryUrl."));
        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(Guid.NewGuid(), assignmentId, request.StudentCode.Trim(),
            request.RepositoryUrl.Trim(), "received", now, now);
        lock (submissionsLock) { submissions.Add(submission); }
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});
app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
