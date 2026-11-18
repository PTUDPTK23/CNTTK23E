using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Xay dung ung dung realtime voi SignalR"),
    new(2, "Bai tap REST", "Thuc hanh REST API")
};
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => "EduSubmit Realtime API is running.");
app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest? request,
     IHubContext<SubmissionHub> hubContext) =>
{
    if (assignments.All(a => a.Id != assignmentId))
        return Results.NotFound(new ApiError("Assignment not found."));

    if (request is null || string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl) ||
        !Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repoUri) ||
        (repoUri.Scheme != Uri.UriSchemeHttp && repoUri.Scheme != Uri.UriSchemeHttps))
        return Results.BadRequest(new ApiError("studentCode and a valid repositoryUrl are required."));

    var submission = new SubmissionDto(
        Guid.NewGuid(), assignmentId, request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(), "received", DateTimeOffset.UtcNow);

    lock (submissionsLock) submissions.Add(submission);

    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(s => s.Id == id);
        return submission is null ? Results.NotFound(new ApiError("Submission not found.")) : Results.Ok(submission);
    }
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(
    Guid id, List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(
    Guid id, string status, List<SubmissionDto> list, object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        var index = list.FindIndex(s => s.Id == id);
        if (index >= 0)
        {
            updated = list[index] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[index] = updated;
        }
    }

    if (updated is null) return;
    var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", message);
    Console.WriteLine($"[Push] submission:{id} -> {status}");
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string id)
    {
        if (!Guid.TryParse(id, out var submissionId))
            throw new HubException("Invalid submission id.");

        var group = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId} theo doi {group}");
        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, string Description);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Message);
