// ==============================================================================
// EDUSUBMIT REALTIME - BÀI 3: GIAO TIẾP THỜI GIAN THỰC VỚI SIGNALR
// Submission.RealtimeApi - ASP.NET Core 10 Minimal API + SignalR Hub
// Cổng hoạt động mặc định: http://localhost:5000
// ==============================================================================

using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR vào DI Container
builder.Services.AddSignalR();

var app = builder.Build();

// ── DỮ LIỆU MẪU TRONG BỘ NHỚ (IN-MEMORY) ──────────────────────────────────────
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Xay dung kenh thong bao thoi gian thuc voi SignalR", DateTimeOffset.UtcNow.AddDays(7), true),
    new(2, "Bai tap RESTful API", "Thiet ke RESTful API cho he thong nop bai", DateTimeOffset.UtcNow.AddDays(3), true)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // Khóa an toàn đồng bộ giữa tác vụ nền và HTTP request

// ── ENDPOINT GỐC: KIỂM TRA TRẠNG THÁI SERVER ─────────────────────────────────
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// ── NHÓM RESTFUL API: QUẢN LÝ BÀI TẬP VÀ LƯỢT NỘP ────────────────────────────

// GET /api/assignments : Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : Lấy thông tin chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap voi ID = {assignmentId}."))
        : Results.Ok(assignment);
});

// POST /api/assignments/{assignmentId:int}/submissions : Nộp bài và kích hoạt chấm nền
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap voi ID = {assignmentId}."));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", $"Bai tap '{assignment.Title}' da dong, khong the nop bai."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma sinh vien va dia chi repository khong duoc de trong."));
    }

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode.Trim(),
        RepositoryUrl: request.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: now,
        UpdatedAt: now
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khởi động tác vụ chấm nền giả lập, không await (Task chạy nền)
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    // Trả về HTTP 201 Created kèm header Location và dữ liệu lượt nộp ban đầu
    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{submissionId:guid} : Đọc trạng thái lượt nộp (Nguồn sự thật - Source of Truth)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Khong tim thay luot nop voi ID = {submissionId}."))
            : Results.Ok(submission);
    }
});

// ── ÁNH XẠ SIGNALR HUB ────────────────────────────────────────────────────────
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// ── TÁC VỤ CHẤM BÀI NỀN (BACKGROUND GRADING WORKER) ───────────────────────────
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    // Giả lập xử lý chấm bài: sau 1s chuyển sang processing, sau thêm 2s chuyển sang graded
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status, List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate)
    {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = list[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            list[i] = updated;
        }
    }

    if (updated is null) return;

    Console.WriteLine($"[Push] submission:{id} -> {status}");
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);

    // Đẩy thông báo thời gian thực về ĐÚNG nhóm submission:{id}
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// ── SIGNALR HUB: ĐĂNG KÝ THEO DÕI LƯỢT NỘP ────────────────────────────────────
sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi submission:{submissionId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// ── KHAI BÁO DTO VÀ HỢP ĐỒNG DỮ LIỆU CỦA MÁY CHỦ (SERVER DTOs) ────────────────
public record AssignmentDto(int Id, string Title, string Description, DateTimeOffset Deadline, bool IsOpen);

public record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

public record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);

public record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt
);

public record ApiError(string Code, string Message);
