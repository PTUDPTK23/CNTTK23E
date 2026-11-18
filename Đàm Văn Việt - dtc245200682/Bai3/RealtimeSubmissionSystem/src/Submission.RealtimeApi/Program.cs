using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "REST va thong bao trang thai realtime", true),
    new(2, "Bai tap Client", "Theo doi nhieu luot nop", true),
    new(3, "Bai tap da dong", "Thu ASSIGNMENT_CLOSED", false)
};
var submissions = new List<SubmissionDto>();
var gate = new object();
app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments));
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hub, IHostApplicationLifetime lifetime, ILoggerFactory loggerFactory) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));
    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(Guid.NewGuid(), assignmentId, request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(), "received", now, now);
    lock (gate) { submissions.Add(submission); }
    _ = SimulateGradingAsync(submission, submissions, gate, hub, lifetime.ApplicationStopping,
        loggerFactory.CreateLogger("Grading"));
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (gate)
    {
        var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});
app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

static async Task SimulateGradingAsync(SubmissionDto created, List<SubmissionDto> list, object gate,
    IHubContext<SubmissionHub> hub, CancellationToken stopping, ILogger logger)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(1), stopping);
        await UpdateStatusAsync(created.Id, "processing", list, gate, hub, stopping, logger);
        await Task.Delay(TimeSpan.FromSeconds(2), stopping);
        var finalStatus = created.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "graded" : "failed";
        await UpdateStatusAsync(created.Id, finalStatus, list, gate, hub, stopping, logger);
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    catch (Exception ex) { logger.LogError(ex, "Khong cham duoc luot nop {Id}", created.Id); }
}

static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> list, object gate,
    IHubContext<SubmissionHub> hub, CancellationToken stopping, ILogger logger)
{
    SubmissionDto updated;
    lock (gate)
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i < 0) return;
        updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        list[i] = updated;
    }
    // Lưu trạng thái trước khi phát; lỗi gửi tin không làm hỏng luồng chấm nền.
    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
    try
    {
        await hub.Clients.Group(SubmissionGroups.ForSubmission(id)).SendAsync("SubmissionStatusChanged",
            new SubmissionStatusMessage(id, status, updated.UpdatedAt), stopping);
    }
    catch (Exception ex) when (!stopping.IsCancellationRequested)
    {
        logger.LogWarning(ex, "Da luu {Status} nhung khong day duoc tin {Id}", status, id);
    }
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id))
            throw new HubException("INVALID_SUBMISSION_ID: Ma luot nop phai la GUID.");
        await Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.ForSubmission(id));
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {SubmissionGroups.ForSubmission(id)}");
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id:D}";
}
