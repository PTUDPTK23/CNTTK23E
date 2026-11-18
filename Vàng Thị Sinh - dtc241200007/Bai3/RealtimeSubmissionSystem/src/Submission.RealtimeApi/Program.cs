using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new { service = "SubmissionApi", status = "running" }));

// 1. API lấy danh sách bài tập (giữ từ Bài 2)
app.MapGet("/api/assignments", () => Results.Ok(new[]
{
    new AssignmentDto(1, "Bài tập SignalR", "Lập trình ứng dụng phân tán với SignalR")
}));

// 2. API tạo lượt nộp bài (POST) - Cập nhật thêm IHubContext và tác vụ nền
app.MapPost("/api/assignments/{assignmentId:int}/submissions", (
    int assignmentId, 
    CreateSubmissionRequest request,
    IHubContext<SubmissionHub> hubContext) =>
{
    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_REQUEST", "Thiếu thông tin mã sinh viên hoặc đường dẫn repository."));
    }

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode,
        request.RepositoryUrl,
        "received",
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Khởi động tác vụ nền chấm bài, KHÔNG dùng await để POST trả về 201 ngay lập tức
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// 3. API đọc trạng thái lượt nộp theo ID (có UpdatedAt)
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Không tìm thấy lượt nộp."))
            : Results.Ok(submission);
    }
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// --- CÁC PHƯƠNG THỨC PHỤ TRỢ (NẰM TRÊN CÁC TYPE KHAI BÁO) ---
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list, 
    object gate, IHubContext<SubmissionHub> hub) {
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));
    await UpdateStatusAsync(id, "graded", list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status, 
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub) {
    SubmissionDto? updated = null;
    lock (gate) {
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0) { updated = list[i] with { Status = status, 
                                            UpdatedAt = DateTimeOffset.UtcNow };
                      list[i] = updated; }
    }
    if (updated is null) return;
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, 
                                          updated.UpdatedAt);
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
             .SendAsync("SubmissionStatusChanged", msg);
}

// --- TOÀN BỘ KHAI BÁO CLASS VÀ RECORD ĐẶT Ở CUỐI TỆP ---
sealed class SubmissionHub : Hub 
{
    public Task WatchSubmission(string submissionId) 
    {
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

record AssignmentDto(int Id, string Title, string Description);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid Id, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);