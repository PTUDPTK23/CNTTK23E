using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình cổng 5000
builder.WebHost.UseUrls("http://localhost:5000");

// Đăng ký SignalR
builder.Services.AddSignalR();

// Đăng ký CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseCors();

// Bộ nhớ trong (In-memory storage) và lock
var assignments = new List<Assignment>
{
    new() { Id = 1, Title = "Bài 1 - Cài đặt môi trường và socket TCP", Deadline = DateTime.UtcNow.AddDays(7), IsOpen = true },
    new() { Id = 2, Title = "Bài 2 - RESTful API Submission System", Deadline = DateTime.UtcNow.AddDays(7), IsOpen = true },
    new() { Id = 3, Title = "Bài 3 - Giao tiếp thời gian thực với SignalR", Deadline = DateTime.UtcNow.AddDays(7), IsOpen = true },
    new() { Id = 99, Title = "Bài tập đã đóng hạn nộp", Deadline = DateTime.UtcNow.AddDays(-1), IsOpen = false }
};

var submissions = new Dictionary<Guid, Submission>();
var submissionsLock = new object();

// Endpoint GET /
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running",
    version = "1.0",
    hub = "/hubs/submissions",
    timestamp = DateTime.UtcNow
}));

// Endpoint GET /api/assignments
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Endpoint GET /api/assignments/{id:int}
app.MapGet("/api/assignments/{id:int}", (int id) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == id);
    return assignment is not null ? Results.Ok(assignment) : Results.NotFound(new { message = $"Không tìm thấy bài tập {id}" });
});

// Endpoint GET /api/submissions/{id:guid}
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    lock (submissionsLock)
    {
        if (submissions.TryGetValue(id, out var sub))
        {
            return Results.Ok(ToDto(sub));
        }
    }
    return Results.NotFound(new { message = $"Không tìm thấy bài nộp với mã {id}" });
});

// Endpoint POST /api/assignments/{assignmentId:int}/submissions
app.MapPost("/api/assignments/{assignmentId:int}/submissions", async (
    int assignmentId,
    CreateSubmissionDto dto,
    IHubContext<SubmissionsHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment == null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy bài tập với Id = {assignmentId}" });
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new { message = "Bài tập đã đóng, không thể nộp bài." });
    }

    if (string.IsNullOrWhiteSpace(dto.StudentCode) || string.IsNullOrWhiteSpace(dto.RepoUrl))
    {
        return Results.BadRequest(new { message = "Mã sinh viên và RepoUrl không được để trống." });
    }

    var submission = new Submission
    {
        Id = Guid.NewGuid(),
        AssignmentId = assignmentId,
        StudentCode = dto.StudentCode.Trim(),
        StudentName = dto.StudentName?.Trim() ?? string.Empty,
        RepoUrl = dto.RepoUrl.Trim(),
        Status = "received",
        SubmittedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    lock (submissionsLock)
    {
        submissions[submission.Id] = submission;
    }

    // Bắt đầu tiến trình chấm điểm giả lập nền (background task)
    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", ToDto(submission));
});

// Endpoint tương thích POST /api/submissions
app.MapPost("/api/submissions", async (
    CreateSubmissionDto dto,
    IHubContext<SubmissionsHub> hubContext) =>
{
    var assignmentId = dto.AssignmentId ?? 3;
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);
    if (assignment == null)
    {
        return Results.NotFound(new { message = $"Không tìm thấy bài tập với Id = {assignmentId}" });
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new { message = "Bài tập đã đóng, không thể nộp bài." });
    }

    if (string.IsNullOrWhiteSpace(dto.StudentCode) || string.IsNullOrWhiteSpace(dto.RepoUrl))
    {
        return Results.BadRequest(new { message = "Mã sinh viên và RepoUrl không được để trống." });
    }

    var submission = new Submission
    {
        Id = Guid.NewGuid(),
        AssignmentId = assignmentId,
        StudentCode = dto.StudentCode.Trim(),
        StudentName = dto.StudentName?.Trim() ?? string.Empty,
        RepoUrl = dto.RepoUrl.Trim(),
        Status = "received",
        SubmittedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    lock (submissionsLock)
    {
        submissions[submission.Id] = submission;
    }

    _ = SimulateGradingAsync(submission.Id, submissions, submissionsLock, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", ToDto(submission));
});

// Map SignalR Hub
app.MapHub<SubmissionsHub>("/hubs/submissions");

Console.WriteLine("========================================");
Console.WriteLine("   EDUSUBMIT REALTIME SERVER ĐANG CHẠY");
Console.WriteLine("   URL: http://localhost:5000");
Console.WriteLine("   Hub: http://localhost:5000/hubs/submissions");
Console.WriteLine("========================================");

app.Run();

// Hàm giả lập chấm bài trong nền
static async Task SimulateGradingAsync(
    Guid submissionId,
    Dictionary<Guid, Submission> submissions,
    object submissionsLock,
    IHubContext<SubmissionsHub> hubContext)
{
    // Bước 1: Sau 1 giây -> chuyển sang 'processing'
    await Task.Delay(1000);
    Submission? sub;
    lock (submissionsLock)
    {
        if (submissions.TryGetValue(submissionId, out sub))
        {
            sub.Status = "processing";
            sub.UpdatedAt = DateTime.UtcNow;
        }
    }

    if (sub != null)
    {
        await hubContext.Clients.Group($"submission:{submissionId}")
            .SendAsync("SubmissionStatusChanged", new StatusUpdateMessage(
                sub.Id,
                sub.Status,
                sub.Grade,
                "Đang tiến hành chấm bài và kiểm tra mã nguồn tự động...",
                sub.UpdatedAt
            ));
    }

    // Bước 2: Sau tiếp 2 giây -> chuyển sang 'graded'
    await Task.Delay(2000);
    lock (submissionsLock)
    {
        if (submissions.TryGetValue(submissionId, out sub))
        {
            sub.Status = "graded";
            sub.Grade = 9.5;
            sub.Feedback = "Bài làm xuất sắc! Tất cả test case đều passed.";
            sub.GradedAt = DateTime.UtcNow;
            sub.UpdatedAt = DateTime.UtcNow;
        }
    }

    if (sub != null)
    {
        await hubContext.Clients.Group($"submission:{submissionId}")
            .SendAsync("SubmissionStatusChanged", new StatusUpdateMessage(
                sub.Id,
                sub.Status,
                sub.Grade,
                sub.Feedback,
                sub.UpdatedAt
            ));
    }
}

static SubmissionDto ToDto(Submission s) => new(
    s.Id,
    s.AssignmentId,
    s.StudentCode,
    s.StudentName,
    s.RepoUrl,
    s.Status,
    s.Grade,
    s.Feedback,
    s.SubmittedAt,
    s.GradedAt,
    s.UpdatedAt
);

public class SubmissionsHub : Hub
{
    private readonly ILogger<SubmissionsHub> _logger;

    public SubmissionsHub(ILogger<SubmissionsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client đã kết nối SubmissionsHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public async Task WatchSubmission(string submissionId)
    {
        var groupName = $"submission:{submissionId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} tham gia nhóm theo dõi: {GroupName}", Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("WatchAcknowledged", new { submissionId, groupName, message = "Đang theo dõi tiến trình chấm bài." });
    }
}

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public bool IsOpen { get; set; }
}

public class Submission
{
    public Guid Id { get; set; }
    public int AssignmentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string RepoUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "received";
    public double? Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public record CreateSubmissionDto(int? AssignmentId, string StudentCode, string? StudentName, string RepoUrl);

public record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string StudentName,
    string RepoUrl,
    string Status,
    double? Grade,
    string? Feedback,
    DateTime SubmittedAt,
    DateTime? GradedAt,
    DateTime UpdatedAt
);

public record StatusUpdateMessage(
    Guid SubmissionId,
    string Status,
    double? Grade,
    string? Feedback,
    DateTime UpdatedAt
);
