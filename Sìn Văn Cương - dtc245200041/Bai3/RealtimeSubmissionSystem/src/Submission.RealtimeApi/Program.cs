using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ SignalR vào DI container (bắt buộc đặt trước builder.Build())
builder.Services.AddSignalR();

var app = builder.Build();

// 2. Dữ liệu mẫu trong bộ nhớ (kế thừa Bài 2)
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Xay dung he thong thong bao trang thai nop bai thoi gian thuc", true),
    new(2, "Bai tap REST API", "Thiet ke endpoint va kiem thu hop dong API", true),
    new(3, "Bai tap da dong", "Bai tap da het han nop", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object(); // Khóa đồng bộ giữa luồng HTTP và tác vụ chấm nền

// 3. Các endpoint RESTful (kế thừa Bài 2 + bổ sung UpdatedAt & lock)

// GET / : Kiểm tra dịch vụ đang hoạt động
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// GET /api/assignments : Lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// GET /api/assignments/{assignmentId:int} : Lấy chi tiết một bài tập
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// POST /api/assignments/{assignmentId:int}/submissions : Tạo lượt nộp mới và kích hoạt chấm nền
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) =>
    {
        // Cổng kiểm tra 1: Bài tập có tồn tại không? -> 404
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

        // Cổng kiểm tra 2: Bài tập còn mở không? -> 400
        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop."));

        // Cổng kiểm tra 3: Dữ liệu đầu vào có hợp lệ không? -> 400
        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError(
                "INVALID_SUBMISSION",
                "Ma sinh vien va duong dan kho ma nguon khong duoc de trong."));
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

        // Khởi động tác vụ chấm bài ở nền (không await để trả 201 Created ngay lập tức)
        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// GET /api/submissions/{submissionId:guid} : Nguồn sự thật đọc lại trạng thái lượt nộp
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

// 4. Ánh xạ SignalR Hub vào tuyến /hubs/submissions (bắt buộc đặt trước app.Run())
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// 5. Các hàm xử lý tác vụ nền, Hub và khai báo kiểu (đặt sau app.Run() để tránh lỗi CS8803)

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

    // Lưu trạng thái mới trước rồi mới đẩy thông báo (đảm bảo tính nhất quán khi client gọi GET)
    lock (gate)
    {
        var index = list.FindIndex(s => s.Id == id);
        if (index >= 0)
        {
            updated = list[index] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            list[index] = updated;
        }
    }

    if (updated is null) return;

    var groupName = SubmissionGroups.ForSubmission(id);
    var msg = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);

    Console.WriteLine($"[Push] {groupName} -> {updated.Status}");
    await hub.Clients.Group(groupName).SendAsync("SubmissionStatusChanged", msg);
}

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}");
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

// Các bản sao hợp đồng DTO
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
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