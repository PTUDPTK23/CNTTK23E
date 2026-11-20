
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

// Du lieu bai tap
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR",
        "Thuc hanh giao tiep thoi gian thuc", true),

    new(2, "Bai tap Client",
        "Viet console client", true)
};

// Danh sach luot nop
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// Kiem tra may chu
app.MapGet("/", () => Results.Ok(
    new { service = "Submission.RealtimeApi", status = "running" }));

// GET danh sach bai tap
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// GET chi tiet bai tap
app.MapGet("/api/assignments/{assignmentId:int}",
    (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    return assignment is null
        ? Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap"))
        : Results.Ok(assignment);
});

// GET trang thai luot nop
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            i => i.Id == submissionId);

        return submission is null
            ? Results.NotFound(new ApiError(
                "SUBMISSION_NOT_FOUND",
                "Khong tim thay luot nop"))
            : Results.Ok(submission);
    }
});

// POST tao luot nop
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    // 404: Khong ton tai bai tap
    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap"));
    }

    // 400: Bai tap da dong
    if (!assignment.IsActive)
    {
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED",
            "Bai tap da dong"));
    }

    // 400: Du lieu khong hop le
    if (request is null ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Thieu StudentCode hoac RepositoryUrl"));
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

    // Khoi dong cham bai o nen, khong cho
    _ = SimulateGradingAsync(
        submission.Id,
        submissions,
        submissionsLock,
        hubContext);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// SignalR Hub
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// Buoc 4.2: Mo phong cham bai
static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));

    await UpdateStatusAsync(
        id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));

    await UpdateStatusAsync(
        id, "graded", list, gate, hub);
}

// Cap nhat trang thai va gui thong bao
static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;

    // 1. Cap nhat du lieu truoc
    lock (gate)
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

    // 2. Tao thong diep SignalR
    var msg = new SubmissionStatusMessage(
        updated.Id,
        updated.Status,
        updated.UpdatedAt);

    // 3. Gui dung nhom submission:id
    await hub.Clients.Group(
        SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);

    Console.WriteLine(
        $"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
}

// DTO
record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsActive);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);

// Thong diep trang thai
record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

// Hub theo doi luot nop
sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

// Quan ly ten nhom
static class SubmissionGroups
{
    public static string ForSubmission(Guid id)
        => ForSubmission(id.ToString());

    public static string ForSubmission(string id)
        => $"submission:{id}";
}
