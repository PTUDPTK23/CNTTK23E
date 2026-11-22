// Bài 3 – EduSubmit lát cắt 3 (PI 3.3): RESTful API + SignalR hub (giáo trình Mã nguồn 3.3)
// Chạy: dotnet run --project src/Submission.RealtimeApi --urls http://localhost:5000
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// [MỚI] Việc 1: đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu tạm trong bộ nhớ (giống Bài 2)
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap SignalR", "Nhan thong bao trang thai bai nop theo thoi gian thuc", true),
    new(2, "Bai tap REST API", "On lai API assignment va submission", true)
};

var submissions = new List<SubmissionDto>();

// [MỚI] Khóa: tác vụ chấm nền và các yêu cầu HTTP cùng sửa danh sách
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new {
    service = "Submission.RealtimeApi",
    status = "running"
}));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock) {
        var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

// POST tạo lượt nộp – giữ nguyên Bài 2, chỉ thêm tham số IHubContext và 1 dòng khởi động chấm nền
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) => { // [MỚI] "loa phát" dùng được ngoài hub
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null) {
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        }
        if (!assignment.IsOpen) {
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong nop bai."));
        }
        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl)) {
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac repository."));
        }

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

        // [MỚI] Khởi động chấm bài ở nền, KHÔNG chờ (dấu _ = bỏ qua Task) -> trả 201 ngay
        _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext, submission.RepositoryUrl);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// [MỚI] Việc 2: ánh xạ hub
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// [MỚI] Mô phỏng bộ chấm: 1 giây -> processing, 2 giây nữa -> graded (hoặc failed nếu repo không bắt đầu bằng https:// theo Bài tập 2)
static async Task SimulateGradingAsync(
    Guid submissionId,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext,
    string repositoryUrl = "") {
    await Task.Delay(TimeSpan.FromSeconds(1));
    await UpdateStatusAsync(submissionId, "processing", submissions, submissionsLock, hubContext);

    await Task.Delay(TimeSpan.FromSeconds(2));
    string finalStatus = (!string.IsNullOrEmpty(repositoryUrl) && !repositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        ? "failed"
        : "graded";
    await UpdateStatusAsync(submissionId, finalStatus, submissions, submissionsLock, hubContext);
}

// [MỚI] Cầu nối: cập nhật dữ liệu REST TRƯỚC, rồi mới đẩy thông báo SignalR tới đúng nhóm
static async Task UpdateStatusAsync(
    Guid submissionId,
    string status,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext) {
    SubmissionDto? updatedSubmission = null;
    lock (submissionsLock) {
        var index = submissions.FindIndex(item => item.Id == submissionId);
        if (index >= 0) {
            updatedSubmission = submissions[index] with {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            submissions[index] = updatedSubmission;
        }
    }

    if (updatedSubmission is null) {
        return;
    }

    var message = new SubmissionStatusMessage(
        updatedSubmission.Id,
        updatedSubmission.Status,
        updatedSubmission.UpdatedAt);

    Console.WriteLine($"[Push] {SubmissionGroups.ForSubmission(submissionId)} -> {status}");

    await hubContext.Clients
        .Group(SubmissionGroups.ForSubmission(submissionId))
        .SendAsync("SubmissionStatusChanged", message);
}

// [MỚI] Hub rất mỏng: chỉ cho máy khách đăng ký theo dõi một lượt nộp
sealed class SubmissionHub : Hub {
    public Task WatchSubmission(string submissionId) {
        Console.WriteLine($"[Hub] {Context.ConnectionId[..8]} theo doi submission:{submissionId}");
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

// [MỚI] Một nơi duy nhất quy định tên nhóm: "submission:{id}"
static class SubmissionGroups {
    public static string ForSubmission(Guid submissionId) {
        return ForSubmission(submissionId.ToString());
    }
    public static string ForSubmission(string submissionId) {
        return $"submission:{submissionId}";
    }
}

record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt); // [MỚI] thời điểm đổi trạng thái gần nhất

// [MỚI] Hợp đồng thông điệp đẩy: chỉ chứa dữ liệu tối thiểu để cập nhật giao diện
record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);
