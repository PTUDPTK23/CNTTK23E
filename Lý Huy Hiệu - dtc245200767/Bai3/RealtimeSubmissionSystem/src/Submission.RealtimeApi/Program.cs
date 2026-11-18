using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://localhost:5000");
builder.Services.AddSignalR();

var app = builder.Build();
var assignments = new[]
{
    new AssignmentDto(1, "Giao tiep thoi gian thuc voi SignalR", true),
    new AssignmentDto(2, "Bai tap da dong", false)
};
var submissions = new Dictionary<Guid, SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments));
app.MapGet("/api/submissions/{submissionId:guid}", IResult (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        return submissions.TryGetValue(submissionId, out var submission)
            ? Results.Ok(submission)
            : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));
    }
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    IResult (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = Array.Find(assignments, item => item.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Can studentCode va repositoryUrl."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(Guid.NewGuid(), assignmentId, request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(), "received", now, now);
    lock (submissionsLock)
    {
        submissions.Add(submission.Id, submission);
    }

    _ = SimulateGradingAsync(submission, hubContext);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

async Task SimulateGradingAsync(SubmissionDto submission, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(submission.Id, "processing", hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(submission.Id, "graded", hub);
}

async Task UpdateStatusAsync(Guid id, string status, IHubContext<SubmissionHub> hub)
{
    SubmissionDto updated;
    lock (submissionsLock)
    {
        updated = submissions[id] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        submissions[id] = updated;
    }

    var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    try
    {
        await hub.Clients.Group(SubmissionHub.GroupFor(id)).SendAsync("SubmissionStatusChanged", message);
        Console.WriteLine($"[Push] {id} -> {status}");
    }
    catch (Exception error)
    {
        app.Logger.LogWarning(error, "Khong gui duoc trang thai {Status} cho luot nop {SubmissionId}", status, id);
    }
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(Guid submissionId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(submissionId));

    internal static string GroupFor(Guid submissionId) => $"submission:{submissionId}";
}

sealed record AssignmentDto(int Id, string Title, bool IsOpen);
sealed record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
sealed record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
sealed record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
sealed record ApiError(string Code, string Message);
