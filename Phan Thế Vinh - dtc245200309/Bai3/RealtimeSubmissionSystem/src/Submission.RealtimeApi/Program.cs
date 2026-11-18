using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Giao tiep thoi gian thuc voi SignalR", DateTimeOffset.UtcNow.AddDays(7)),
    new(2, "Bai tap RESTful API", "Xay dung RESTful API voi ASP.NET Core", DateTimeOffset.UtcNow.AddDays(14))
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(
    new { service = "SubmissionRealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Submission {submissionId} not found"))
            : Results.Ok(submission);
    }
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Assignment {assignmentId} not found"));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "StudentCode and RepositoryUrl are required"));
    }

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        now,
        now
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khoi dong cham bai o nen, KHONG cho
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext, submission.RepositoryUrl);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub, string repositoryUrl)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));
    // Bai tap van dung: neu RepositoryUrl khong bat dau bang https:// -> failed, nguoc lai graded
    string finalStatus = repositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "graded"
        : "failed";
    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }

    if (updated is null) return;
    Console.WriteLine($"[Push] submission:{id} -> {status}");
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, string Description, DateTimeOffset Deadline);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
