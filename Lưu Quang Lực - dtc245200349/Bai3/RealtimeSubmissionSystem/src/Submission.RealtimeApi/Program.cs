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
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
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
        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock,
            hubContext, app.Logger, app.Lifetime.ApplicationStopping);
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


static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list, object gate,
    IHubContext<SubmissionHub> hub, ILogger logger, CancellationToken stopping)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(1), stopping);
        await UpdateStatusAsync(id, "processing", list, gate, hub, stopping);
        await Task.Delay(TimeSpan.FromSeconds(2), stopping);
        await UpdateStatusAsync(id, "graded", list, gate, hub, stopping);
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    catch (Exception ex) { logger.LogError(ex, "Grading failed for {SubmissionId}", id); }
}

static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub, CancellationToken stopping)
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
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id)).SendAsync("SubmissionStatusChanged",
        new SubmissionStatusMessage(id, updated.Status, updated.UpdatedAt), stopping);
    Console.WriteLine($"[Push] submission:{id} -> {updated.Status}");
}

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
