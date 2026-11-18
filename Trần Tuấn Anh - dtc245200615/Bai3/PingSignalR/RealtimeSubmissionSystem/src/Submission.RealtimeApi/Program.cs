using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR vào DI (trước builder.Build)
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", true),
    new(2, "Bai tap da dong", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// 1. Kiểm tra dịch vụ hoạt động
app.MapGet("/", () => Results.Ok(
    new { service = "Submission.RealtimeApi", status = "running" }));

// 2. Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 3. Tạo lượt nộp mới + kích hoạt chấm bài nền qua IHubContext
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest? request,
     IHubContext<SubmissionHub> hubContext) =>
    {
        var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
        {
            return Results.NotFound(new ApiError(
                "ASSIGNMENT_NOT_FOUND",
                $"Khong tim thay bai tap co ma {assignmentId}."));
        }

        if (!assignment.IsOpen)
        {
            return Results.BadRequest(new ApiError(
                "ASSIGNMENT_CLOSED",
                "Bai tap nay da dong, khong nhan luot nop moi."));
        }

        if (request is null ||
            string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError(
                "INVALID_SUBMISSION",
                "Thieu studentCode hoac repositoryUrl."));
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

        // Khởi động chấm bài ở nền, KHÔNG await để trả 201 ngay
        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// 4. Đọc trạng thái hiện tại của lượt nộp (REST là nguồn sự thật)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

// Ánh xạ hub SignalR vào tuyến /hubs/submissions (trước app.Run)
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// --- Các hàm chạy nền, Hub và DTO đặt ở cuối tệp ---

static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
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

    var groupName = SubmissionGroups.ForSubmission(id);
    Console.WriteLine($"[Push] {groupName} -> {status}");

    var msg = new SubmissionStatusMessage(
        updated.Id,
        updated.Status,
        updated.UpdatedAt);

    await hub.Clients.Group(groupName)
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

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);