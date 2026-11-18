using Microsoft.AspNetCore.SignalR;
<<<<<<< HEAD

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
    var finalStatus = submission.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "graded"
        : "failed";
    await UpdateStatusAsync(submission.Id, finalStatus, hub);
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
=======
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<SubmissionStore>();

var app = builder.Build();

app.MapGet("/", () => "EduSubmit API is running");

app.MapPost("/api/submissions", async (
    CreateSubmissionRequest request,
    SubmissionStore store,
    IHubContext<SubmissionHub> hub) =>{
    if (string.IsNullOrWhiteSpace(request.StudentId))
        return Results.BadRequest("StudentId is required");

    var submission = new Submission
    {
        Id = Guid.NewGuid(),
        StudentId = request.StudentId,
        Status = "submitted",
        UpdatedAt = DateTimeOffset.UtcNow
    };

    store.Items[submission.Id] = submission;
await hub.Clients.All.SendAsync(
    "SubmissionStatusChanged",
    submission.StudentId,
    submission.Status);

    return Results.Created(
        $"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{id:guid}",
    (Guid id, SubmissionStore store) =>
{
    return store.Items.TryGetValue(id, out var submission)
        ? Results.Ok(submission)
        : Results.NotFound();
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

record CreateSubmissionRequest(string StudentId);

class Submission
{
    public Guid Id { get; set; }
    public string StudentId { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}

class SubmissionStore
{
    public ConcurrentDictionary<Guid, Submission> Items { get; } = new();
}
>>>>>>> d93af16 (Bai 3 - SignalR - DTC245200767)
