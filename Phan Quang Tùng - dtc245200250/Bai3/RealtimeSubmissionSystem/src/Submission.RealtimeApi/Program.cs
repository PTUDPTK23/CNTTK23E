using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ SignalR vào DI
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu mẫu bài tập trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Giao tiep thoi gian thuc...", true),
    new(2, "Bai tap Client", "Viet console client...", true),
    new(3, "Bai tap Mo rong (da dong)", "Kiem thu trang thai dong...", false)
};

// Dữ liệu các lượt nộp bài trong bộ nhớ
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// 1. GET / : Kiểm tra máy chủ đang chạy
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// 2. GET /api/assignments : Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : Chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// 3. POST /api/assignments/{assignmentId:int}/submissions : Nộp bài (REST 201 + kích hoạt chấm ngầm SignalR)
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

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

    // Khởi động chấm bài ở nền, KHÔNG chờ (Task chưa chờ)
    _ = SimulateGradingAsync(submission.Id, submission.RepositoryUrl, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 4. GET /api/submissions/{submissionId:guid} : Đọc lại lượt nộp (Nguồn sự thật)
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

// Hỗ trợ GET /api/submissions (id rỗng -> 404 cho ca kiểm thử 7)
app.MapGet("/api/submissions", () => Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Vui long cung cap ma luot nop.")));

// 5. Ánh xạ Hub SignalR vào tuyến /hubs/submissions
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// Mô phỏng chấm bài ngầm: 1s -> processing, 2s -> graded/failed
static async Task SimulateGradingAsync(Guid id, string repoUrl, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));
    string finalStatus = (repoUrl != null && repoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        ? "graded"
        : "failed";
    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (gate) // ① Cập nhật dữ liệu vào nguồn sự thật trước
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
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt); // ② Dữ liệu tối thiểu

    // ③ Đẩy thông báo tới đúng nhóm
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

// --- Hub & Helper Classes ---

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi submission:{submissionId}");
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

// --- DTO & Records ---
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
