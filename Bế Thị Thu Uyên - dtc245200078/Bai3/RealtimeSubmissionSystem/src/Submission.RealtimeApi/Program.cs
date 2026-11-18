using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu Bài tập
var assignments = new List<AssignmentDto>
{
    new(1, "Bai 1: REST API co ban", true),
    new(2, "Bai 2: Xay dung EduSubmit REST", true),
    new(3, "Bai 3: Thong bao thoi gian thuc", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => "EduSubmit Realtime API dang chay.");

// 1. Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 2. Nộp bài: lưu trạng thái received, kích hoạt chấm nền (không await) và trả 201 ngay
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId,
    CreateSubmissionRequest req,
    IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Khong tim thay bai tap {assignmentId}."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong han nop."));

    if (string.IsNullOrWhiteSpace(req.StudentCode) || string.IsNullOrWhiteSpace(req.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_INPUT", "StudentCode va RepositoryUrl khong duoc de trong."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: req.StudentCode.Trim(),
        RepositoryUrl: req.RepositoryUrl.Trim(),
        Status: "received",
        ReceivedAt: now,
        UpdatedAt: now
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khởi động chấm bài ở nền, KHÔNG await để trả 201 Created ngay lập tức
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 3. Tra cứu trạng thái lượt nộp
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

// Ánh xạ SignalR Hub
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run("http://localhost:5000");

// Tác vụ nền giả lập quá trình chấm bài (1s -> processing, +2s -> graded)
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

// Cập nhật dữ liệu trong lock TRƯỚC -> Đẩy thông báo tới đúng nhóm SAU
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

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    Console.WriteLine($"[Push] {updated.Id} -> {updated.Status} ({updated.UpdatedAt:HH:mm:ss.ff})");

    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// Hub mỏng: chỉ đưa kết nối vào nhóm theo dõi lượt nộp
sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

// Chuẩn hóa tên nhóm ở một chỗ duy nhất
static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// Các kiểu dữ liệu (DTO / Record)
record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);