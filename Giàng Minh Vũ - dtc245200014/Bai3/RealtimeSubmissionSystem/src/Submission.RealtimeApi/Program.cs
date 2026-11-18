
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// BƯỚC 3: Đăng ký dịch vụ SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Dữ liệu bài tập trong bộ nhớ
var assignments = new List<AssignmentDto>
{
    new(
        1,
        "Bai tap SignalR",
        "Nhan thong bao trang thai bai nop theo thoi gian thuc",
        true
    ),
    new(
        2,
        "Bai tap REST API",
        "On lai API assignment va submission",
        false
    )
};

// Danh sách lượt nộp
var submissions = new List<SubmissionDto>();

// Khóa bảo vệ dữ liệu dùng chung
var submissionsLock = new object();

// GET: Kiểm tra trạng thái API
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// GET: Lấy danh sách bài tập
app.MapGet("/api/assignments", () =>
{
    return Results.Ok(assignments);
});

// GET: Lấy thông tin lượt nộp theo GUID
app.MapGet(
    "/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
    {
        lock (submissionsLock)
        {
            var submission = submissions.FirstOrDefault(
                item => item.Id == submissionId
            );

            return submission is null
                ? Results.NotFound(
                    new ApiError(
                        "SUBMISSION_NOT_FOUND",
                        "Khong tim thay luot nop."
                    )
                )
                : Results.Ok(submission);
        }
    }
);

// POST: Tạo lượt nộp bài
app.MapPost(
    "/api/assignments/{assignmentId:int}/submissions",
    (
        int assignmentId,
        CreateSubmissionRequest request
    ) =>
    {
        // Kiểm tra bài tập tồn tại
        var assignment = assignments.FirstOrDefault(
            item => item.Id == assignmentId
        );

        if (assignment is null)
        {
            return Results.NotFound(
                new ApiError(
                    "ASSIGNMENT_NOT_FOUND",
                    "Khong tim thay bai tap."
                )
            );
        }

        // Kiểm tra bài tập còn mở
        if (!assignment.IsOpen)
        {
            return Results.BadRequest(
                new ApiError(
                    "ASSIGNMENT_CLOSED",
                    "Bai tap da dong nop bai."
                )
            );
        }

        // Kiểm tra dữ liệu đầu vào
        if (
            string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl)
        )
        {
            return Results.BadRequest(
                new ApiError(
                    "INVALID_SUBMISSION",
                    "Thieu ma sinh vien hoac repository."
                )
            );
        }

        // Tạo lượt nộp mới
        var submission = new SubmissionDto(
            Id: Guid.NewGuid(),
            AssignmentId: assignmentId,
            StudentCode: request.StudentCode.Trim(),
            RepositoryUrl: request.RepositoryUrl.Trim(),
            Status: "received",
            ReceivedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow
        );

        // Thêm lượt nộp vào danh sách
        lock (submissionsLock)
        {
            submissions.Add(submission);
        }

        return Results.Created(
            $"/api/submissions/{submission.Id}",
            submission
        );
    }
);

// BƯỚC 3: Ánh xạ SignalR Hub
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();


// =============================================
// BƯỚC 3: SIGNALR HUB
// =============================================

sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId)
        );
    }
}


// =============================================
// QUẢN LÝ TÊN GROUP
// =============================================

static class SubmissionGroups
{
    public static string ForSubmission(Guid submissionId)
    {
        return ForSubmission(submissionId.ToString());
    }

    public static string ForSubmission(string submissionId)
    {
        return $"submission:{submissionId}";
    }
}


// =============================================
// DATA TRANSFER OBJECTS
// =============================================

record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen
);

record CreateSubmissionRequest(
    string? StudentCode,
    string? RepositoryUrl
);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);

record ApiError(
    string Code,
    string Message
);
