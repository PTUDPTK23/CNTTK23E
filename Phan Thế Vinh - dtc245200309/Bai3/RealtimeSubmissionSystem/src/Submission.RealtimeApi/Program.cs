using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<SubmissionStore>();

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Giao tiep thoi gian thuc voi SignalR", DateTimeOffset.UtcNow.AddDays(7)),
    new(2, "Bai tap RESTful API", "Xay dung RESTful API voi ASP.NET Core", DateTimeOffset.UtcNow.AddDays(14))
};

app.MapGet("/", () => Results.Ok(
    new { service = "SubmissionRealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId, SubmissionStore store) =>
{
    lock (store.Gate)
    {
        var submission = store.Submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", $"Submission {submissionId} not found"))
            : Results.Ok(submission);
    }
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     SubmissionStore store,
     IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment is null)
    {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", $"Assignment {assignmentId} not found"));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "StudentCode and RepositoryUrl are required"));
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

    lock (store.Gate)
    {
        store.Submissions.Add(submission);
    }

    // Khoi dong cham bai o nen, KHONG cho
    _ = SimulateGradingAsync(submission.Id, store, hubContext, submission.RepositoryUrl);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(Guid id, SubmissionStore store,
    IHubContext<SubmissionHub> hub, string repositoryUrl)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", store, hub);

    await Task.Delay(TimeSpan.FromSeconds(2));
    // Bai tap van dung: neu RepositoryUrl khong bat dau bang https:// -> failed, nguoc lai graded
    string finalStatus = repositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? "graded"
        : "failed";
    await UpdateStatusAsync(id, finalStatus, store, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    SubmissionStore store, IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;
    lock (store.Gate)
    {
        var i = store.Submissions.FindIndex(s => s.Id == id);
        if (i >= 0)
        {
            updated = store.Submissions[i] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            store.Submissions[i] = updated;
        }
    }

    if (updated is null) return;
    Console.WriteLine($"[Push] submission:{id} -> {status}");
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg);
}

/// <summary>
/// Lớp lưu trữ danh sách lượt nộp và lock đồng bộ cho toàn bộ ứng dụng
/// </summary>
class SubmissionStore
{
    public List<SubmissionDto> Submissions { get; } = new();
    public object Gate { get; } = new();
}

/// <summary>
/// Hub SignalR cho EduSubmit Realtime.
/// LƯU Ý BẢO MẬT GROUP (Slide 67 - Giới hạn hôm nay & Bài học sau):
/// - SignalR Group chỉ là cơ chế giới hạn phạm vi phát thông báo (scoping), KHÔNG tự bảo mật hay phân quyền.
/// - Trong phạm vi Bài 3 (chưa có hệ thống xác thực JWT/OAuth2), server kiểm tra tính hợp lệ của GUID,
///   kiểm tra sự tồn tại thực tế của submissionId và đối chiếu studentCode (nếu có).
/// - Trong hệ thống sản phẩm thật (Bài 8-9): Bắt buộc gắn [Authorize], lấy danh tính đã xác thực
///   từ Context.UserIdentifier (ClaimsPrincipal) và kiểm tra quyền trong CSDL trước khi AddToGroupAsync.
/// </summary>
sealed class SubmissionHub(SubmissionStore store) : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        // 1. Kiểm tra định dạng GUID hợp lệ
        if (!Guid.TryParse(submissionId, out var id))
        {
            Console.WriteLine($"[Hub] Tu choi {Context.ConnectionId[..8]}: ID khong hop le ({submissionId})");
            return;
        }

        // 2. Kiểm tra sự tồn tại của lượt nộp trên máy chủ
        SubmissionDto? submission;
        lock (store.Gate)
        {
            submission = store.Submissions.FirstOrDefault(s => s.Id == id);
        }

        if (submission is null)
        {
            Console.WriteLine($"[Hub] Tu choi {Context.ConnectionId[..8]}: Luot nop khong ton tai ({id})");
            return;
        }

        var groupName = SubmissionGroups.ForSubmission(submissionId);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

record AssignmentDto(int Id, string Title, string Description, DateTimeOffset Deadline);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
