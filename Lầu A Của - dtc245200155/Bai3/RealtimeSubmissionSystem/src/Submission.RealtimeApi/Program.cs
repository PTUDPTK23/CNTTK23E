using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu bài tập mẫu
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Giao tiep thoi gian thuc voi SignalR"),
    new(2, "Bai tap REST", "Thiet ke RESTful API chuan")
};

// Danh sách lượt nộp và khóa đồng bộ luồng
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// 1. GET / : kiểm tra API đang chạy
app.MapGet("/", () => Results.Ok(new { service = "Submission.RealtimeApi", status = "running" }));

// 2. GET /api/assignments : lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 3. POST /api/assignments/{assignmentId}/submissions : nộp bài
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    // Kiểm tra assignment có tồn tại không
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap #{assignmentId}"));
    }

    // Kiểm tra dữ liệu đầu vào
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "StudentCode va RepositoryUrl khong duoc de trong"));
    }

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khởi động chấm bài ở nền, KHÔNG chờ (Slide 52)
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. GET /api/submissions/{submissionId} : đọc chi tiết lượt nộp
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Khong tim thay luot nop #{submissionId}"))
            : Results.Ok(submission);
    }
});

// Ánh xạ hub SignalR
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// ═══════════════════════════════════════════════════════════════
// Tác vụ chấm bài mô phỏng ở nền (Slide 53 & Bài tập 2 Slide 62)
// ═══════════════════════════════════════════════════════════════
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    string targetStatus = "graded";
    lock (gate)
    {
        var sub = list.FirstOrDefault(s => s.Id == id);
        // Bài tập 2: Nếu Repo URL không bắt đầu bằng https:// -> trạng thái cuối là failed
        if (sub != null && !sub.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            targetStatus = "failed";
        }
    }

    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, targetStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate) // ① Cập nhật dữ liệu
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

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt); // ② Dữ liệu tối thiểu
    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg); // ③ Đẩy đúng nhóm
}

// ═══════════════════════════════════════════════════════════════
// SignalR Hub & Groups
// ═══════════════════════════════════════════════════════════════
sealed class SubmissionHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} ket noi den SubmissionHub");
        await base.OnConnectedAsync();
    }

    public Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {SubmissionGroups.ForSubmission(submissionId)}");
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// ═══════════════════════════════════════════════════════════════
// Records & DTOs
// ═══════════════════════════════════════════════════════════════
record AssignmentDto(int Id, string Title, string Description);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
