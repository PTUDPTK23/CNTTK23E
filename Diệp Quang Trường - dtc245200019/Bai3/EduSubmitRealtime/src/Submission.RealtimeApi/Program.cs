using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "open"),
    new(2, "Bai tap da dong", "closed")
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // tác vụ nền + HTTP cùng sửa

app.MapGet("/", () => Results.Ok(
    new { service = "Submission.RealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Khong tim thay luot nop {submissionId}."))
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
            return Results.NotFound(new ApiError(
                "ASSIGNMENT_NOT_FOUND",
                $"Khong tim thay bai tap {assignmentId}."));
        }

        if (!string.Equals(assignment.Status, "open", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new ApiError(
                "ASSIGNMENT_CLOSED",
                $"Bai tap {assignmentId} da dong."));
        }

        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError(
                "INVALID_SUBMISSION",
                "studentCode va repositoryUrl la bat buoc."));
        }

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            request.StudentCode.Trim(),
            request.RepositoryUrl.Trim(),
            "received",
            now,
            now);

        lock (submissionsLock) { submissions.Add(submission); }

        // khởi động chấm bài ở nền, KHÔNG chờ
        _ = SimulateGradingAsync(submission.Id, submissions,
                                 submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)                                     // ① cập nhật dữ liệu
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            list[i] = updated;
        }
    }
    if (updated is null) return;
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status,
                                          updated.UpdatedAt); // ② dữ liệu tối thiểu
    var group = SubmissionGroups.ForSubmission(id);
    Console.WriteLine($"[Push] {group} -> {updated.Status}");
    await hub.Clients.Group(group)
        .SendAsync("SubmissionStatusChanged", msg); // ③ đẩy đúng nhóm
}

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        var group = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {group}");
        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, string Status);

record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);

record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(Guid SubmissionId, string Status,
    DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);
