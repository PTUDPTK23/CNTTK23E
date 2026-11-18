using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR vào Dependency Injection
builder.Services.AddSignalR();

var app = builder.Build();

// Danh sách dữ liệu mẫu lưu trữ trong RAM
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Giao tiep thoi gian thuc voi SignalR va ASP.NET Core", true),
    new(2, "Bai tap REST API", "Thiet ke va kiem thu RESTful API chuan HTTP", true),
    new(3, "Bai tap WebSocket", "Giao thuc WebSocket thuan hai chieu", false) // Bai da dong
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // Khoá bảo vệ danh sách khi tác vụ nền và HTTP cùng cập nhật

// 1. GET / - Kiểm tra trạng thái máy chủ
app.MapGet("/", () => Results.Ok(
    new { service = "Submission.RealtimeApi", status = "running" }));

// 2. GET /api/assignments - Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{id} - Lấy chi tiết bài tập theo ID
app.MapGet("/api/assignments/{id:int}", (int id) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == id);
    return assignment is not null
        ? Results.Ok(assignment)
        : Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap co ID {id}."));
});

// 3. POST /api/assignments/{assignmentId:int}/submissions - Nộp bài (REST 201 + Kích hoạt chấm nền)
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    // Kiểm tra bài tập tồn tại
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap co ID {assignmentId}."));
    }

    // Kiểm tra bài tập còn mở không
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop nua."));
    }

    // Kiểm tra tính hợp lệ của dữ liệu gửi lên
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

    // Khởi động tác vụ chấm bài ở nền, KHÔNG chặn luồng HTTP (non-blocking)
    _ = SimulateGradingAsync(submission.Id, submission.RepositoryUrl, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. GET /api/submissions/{submissionId:guid} - Đọc chi tiết lượt nộp (Nguồn sự thật REST)
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

// 5. Ánh xạ Hub SignalR vào tuyến /hubs/submissions
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// =========================================================================
// SIGNALR HUB & NHÓM THEO DÕI
// =========================================================================

sealed class SubmissionHub : Hub
{
    // Khách gọi để đăng ký nhận thông báo về lượt nộp cụ thể
    public async Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"[Hub] Client {Context.ConnectionId[..8]} dang theo doi nhom {groupName}");
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
        // Sau 1 giây: chuyển sang processing
        await Task.Delay(TimeSpan.FromSeconds(1));
        await UpdateStatusAsync(id, "processing", list, gate, hub);

        // Sau 2 giây tiếp theo: chuyển sang graded (hoặc failed nếu repo không có https://)
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
        lock (gate) // Cập nhật dữ liệu trong RAM trước
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

        Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(id)} -> {status}");
        var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt); // Dữ liệu tối thiểu
        await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
            .SendAsync("SubmissionStatusChanged", msg); // Đẩy tới đúng nhóm khách
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
