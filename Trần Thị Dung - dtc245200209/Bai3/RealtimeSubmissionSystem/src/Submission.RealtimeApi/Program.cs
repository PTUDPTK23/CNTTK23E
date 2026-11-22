using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR vào DI
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu trong bộ nhớ (danh sách bài tập)
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Thiet ke endpoint realtime...", true),
    new(2, "Bai tap Client", "Viet console client...", true),
    new(3, "Bai tap da dong", "Bai tap da het han", false)
};

// Danh sách lưu trữ các lượt nộp bài
var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // tác vụ nền + HTTP cùng sửa

// GET / : Kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// GET /api/assignments : Danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : Chi tiết bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bài tap."))
        : Results.Ok(assignment);
});

// POST /api/assignments/{assignmentId:int}/submissions : Nộp bài (tài nguyên con)
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
(int assignmentId,
 CreateSubmissionRequest request,
 IHubContext<SubmissionHub> hubContext) =>
{
    if (assignmentId <= 0)
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Ma bai tap khong hop le."));

    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

    var duplicated = false;
    lock (submissionsLock)
    {
        duplicated = submissions.Any(item =>
            item.AssignmentId == assignmentId &&
            item.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }
    if (duplicated)
        return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        now,
        now);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khởi động chấm bài ở nền, KHÔNG chờ
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// GET /api/submissions/{submissionId:guid} : Đọc lại lượt nộp theo Id
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

// DELETE /api/submissions/{submissionId:guid} : Xóa lượt nộp
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var s = submissions.FirstOrDefault(i => i.Id == submissionId);
        if (s is null)
            return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop.")) ;

        submissions.Remove(s);
        return Results.NoContent();
    }
});

// Ánh xạ hub vào tuyến /hubs/submissions
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// Giả lập tiến trình chấm bài ở nền (local functions)
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));

    string finalStatus = "graded";
    lock (gate)
    {
        var item = list.FirstOrDefault(s => s.Id == id);
        if (item != null && !item.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            finalStatus = "failed";
        }
    }

    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

// Cập nhật trạng thái vào bộ nhớ trước, rồi mới phát thông điệp
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
    Console.WriteLine($"[Push] submission:{id} -> {status}");
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg); // ③ Đẩy đúng nhóm
}

// Lớp Hub xử lý kết nối thời gian thực
sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi submission:{submissionId}");
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

// Lớp hỗ trợ định danh nhóm
static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// Khai báo các bản ghi DTO
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
