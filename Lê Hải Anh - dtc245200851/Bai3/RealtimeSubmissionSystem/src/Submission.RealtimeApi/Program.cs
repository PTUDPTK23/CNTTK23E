using Microsoft.AspNetCore.SignalR;
// Le Hai Anh - DTC245200851 - CNTT K23E
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint RESTful.", true),
    new(2, "Bai tap Client", "Viet console client HttpClient.", true),
    new(3, "Bai tap da dong", "Dung de kiem tra ASSIGNMENT_CLOSED.", false)
};
var submissionsLock = new object();
var submissions = new List<SubmissionDto>();

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));
app.MapGet("/api/assignments", () => Results.Ok(assignments));
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
{
    // Kiem tra theo thu tu: ton tai -> con mo -> du lieu hop le.
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    var submission = new SubmissionDto(Guid.NewGuid(), assignmentId,
        request.StudentCode.Trim(), request.RepositoryUrl.Trim(), "received", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    lock (submissionsLock) { submissions.Add(submission); }
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);
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

static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        await UpdateStatusAsync(id, "processing", list, gate, hub);
        await Task.Delay(TimeSpan.FromSeconds(2));
        await UpdateStatusAsync(id, "graded", list, gate, hub);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"[Grading] {id}: {exception}");
    }
}

static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto updated;
    lock (gate)
    {
        var index = list.FindIndex(s => s.Id == id);
        if (index < 0) return;
        updated = list[index] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        list[index] = updated;
    }
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id)).SendAsync(
        "SubmissionStatusChanged", new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt));
    Console.WriteLine($"[Push] submission:{id} -> {status}");
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
        => Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.ForSubmission(Guid.Parse(submissionId)));
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => $"submission:{id}";
}

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
