using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

var assignments = new List<AssignmentDto> {
    new(1, "Bai tap SignalR", "Thiet ke endpoint va hub realtime", true),
    new(2, "Bai tap Client", "Viet console client voi HttpClient va SignalR", true),
    new(3, "Bai tap Cu - Da dong", "Bai tap mau da qua han nop", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // tác vụ nền + HTTP cùng sửa

// 1. Kiểm tra trạng thái máy chủ -> 200 OK
app.MapGet("/", () => Results.Ok(new {
    service = "Submission.RealtimeApi",
    status = "running"
}));

// 2. Danh sách bài tập -> 200 OK
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// 3. Chi tiết bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bài tap."))
        : Results.Ok(assignment);
});

// 4. Nộp bài tập: REST POST -> 201 Created + kích hoạt chấm nền
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) => {
        var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Bai tap khong ton tai."));

        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong nhan bai nop."));

        if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl))
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

        var submission = new SubmissionDto(
            Id: Guid.NewGuid(),
            AssignmentId: assignmentId,
            StudentCode: request.StudentCode.Trim(),
            RepositoryUrl: request.RepositoryUrl.Trim(),
            Status: "received",
            ReceivedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);

        lock (submissionsLock) {
            submissions.Add(submission);
        }

        // Khởi động chấm bài ở nền, KHÔNG chờ (Task chưa chờ: _)
        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// 5. Đọc chi tiết lượt nộp theo Id
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock) {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

// 6. Ánh xạ Hub vào tuyến /hubs/submissions
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// ================= Tác vụ chấm bài nền (Local Functions) =================
static async Task SimulateGradingAsync(Guid id, List<SubmissionDto> list,
    object gate, IHubContext<SubmissionHub> hub) {
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(id, "processing", list, gate, hub);
    await Task.Delay(TimeSpan.FromSeconds(2));

    string finalStatus = "graded";
    lock (gate) {
        var item = list.FirstOrDefault(s => s.Id == id);
        if (item != null && !item.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            finalStatus = "failed";
        }
    }
    await UpdateStatusAsync(id, finalStatus, list, gate, hub);
}

static async Task UpdateStatusAsync(Guid id, string status,
    List<SubmissionDto> list, object gate, IHubContext<SubmissionHub> hub) {
    SubmissionDto? updated = null;
    lock (gate) { // 1. Cập nhật dữ liệu vào nguồn sự thật
        var i = list.FindIndex(s => s.Id == id);
        if (i >= 0) {
            updated = list[i] with {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            list[i] = updated;
        }
    }
    if (updated is null) return;

    Console.WriteLine($"[Push] submission:{id} -> {status}");

    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt); // 2. Dữ liệu tối thiểu
    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", msg); // 3. Đẩy thông báo đúng nhóm
}

// ================= Hub & Groups =================
sealed class SubmissionHub : Hub {
    public async Task WatchSubmission(string submissionId) {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        var shortId = Context.ConnectionId.Length >= 8 ? Context.ConnectionId[..8] : Context.ConnectionId;
        Console.WriteLine($"[Hub] {shortId} theo doi {groupName}");
    }
}

static class SubmissionGroups {
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// ================= DTO & Request models =================
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
