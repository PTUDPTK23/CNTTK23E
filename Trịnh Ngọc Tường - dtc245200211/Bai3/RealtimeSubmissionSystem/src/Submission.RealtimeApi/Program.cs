using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký SignalR vào Dependency Injection
builder.Services.AddSignalR();

var app = builder.Build();

// Danh sách dữ liệu mẫu trong RAM
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Giao tiep thoi gian thuc voi SignalR va ASP.NET Core", true),
    new(2, "Bai tap REST API", "Thiet ke va kiem thu RESTful API chuan HTTP", true),
    new(3, "Bai tap WebSocket", "Giao thuc WebSocket thuan hai chieu", false) // Bai da dong
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // Khoa bao ve danh sach khi tac vu nen va HTTP cung sua

// 1. GET / - Kiem tra trang thai dich vu
app.MapGet("/", () => Results.Ok(
    new { service = "Submission.RealtimeApi", status = "running" }));

// 2. GET /api/assignments - Lay danh sach bai tap
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{id} - Lay chi tiet bai tap
app.MapGet("/api/assignments/{id:int}", (int id) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == id);
    return assignment is not null
        ? Results.Ok(assignment)
        : Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap co ID {id}."));
});

// 3. POST /api/assignments/{assignmentId:int}/submissions - Nop bai (REST + kich hoat cham nen)
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    // Kiem tra bai tap ton tai
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap co ID {assignmentId}."));
    }

    // Kiem tra bai tap con mo khong
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop nua."));
    }

    // Kiem tra du lieu hop le
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));
    }

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: now,
        UpdatedAt: now);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khoi dong tac vu cham bai o nen, KHONG cho (non-blocking)
    _ = SimulateGradingAsync(submission.Id, submission.RepositoryUrl, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. GET /api/submissions/{submissionId:guid} - Doc chi tiet luot nop (Nguon su that)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
        return submission is not null
            ? Results.Ok(submission)
            : Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Khong tim thay luot nop co ID {submissionId}."));
    }
});

// Ánh xạ hub SignalR vào tuyến /hubs/submissions
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// =========================================================================
// SIGNALR HUB & HELPER
// =========================================================================

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// =========================================================================
// MÔ PHỎNG CHẤM NỀN (BACKGROUND GRADING WORKER)
// =========================================================================

static partial class Program
{
    public static async Task SimulateGradingAsync(
        Guid id,
        string repoUrl,
        List<SubmissionDto> list,
        object gate,
        IHubContext<SubmissionHub> hub)
    {
        // Sau 1 giay: chuyen sang processing
        await Task.Delay(TimeSpan.FromSeconds(1));
        await UpdateStatusAsync(id, "processing", list, gate, hub);

        // Sau 2 giay tiep theo: chuyen sang graded (hoac failed theo Bai tap 2 Van dung)
        await Task.Delay(TimeSpan.FromSeconds(2));
        var finalStatus = repoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "graded"
            : "failed";
        await UpdateStatusAsync(id, finalStatus, list, gate, hub);
    }

    public static async Task UpdateStatusAsync(
        Guid id,
        string status,
        List<SubmissionDto> list,
        object gate,
        IHubContext<SubmissionHub> hub)
    {
        SubmissionDto? updated = null;
        lock (gate) // Cap nhat du lieu truoc
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

        Console.WriteLine($"[Push] submission:{id} -> {status}");
        var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt); // Du lieu toi thieu
        await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
            .SendAsync("SubmissionStatusChanged", msg); // Day toi dung nhom
    }
}

// =========================================================================
// KHAI BÁO DTO & MODEL
// =========================================================================

public record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
public record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
public record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
public record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
public record ApiError(string Code, string Message);
