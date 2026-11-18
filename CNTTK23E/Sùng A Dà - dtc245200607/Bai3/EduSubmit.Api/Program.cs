using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SubmissionStore>();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
var imageDirectory = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "images"));
if (Directory.Exists(imageDirectory))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imageDirectory),
        RequestPath = "/images"
    });
}

app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapGet("/api/assignments", (SubmissionStore store) => Results.Ok(store.GetAssignments()));

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId, SubmissionStore store) =>
{
    var submission = store.FindSubmission(submissionId);
    return submission is null
        ? (IResult)Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Không tìm thấy lượt nộp."))
        : Results.Ok(submission);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    async (int assignmentId,
        CreateSubmissionRequest request,
        SubmissionStore store,
        IHubContext<SubmissionHub> hubContext) =>
    {
        if (store.FindAssignment(assignmentId) is null)
        {
            return (IResult)Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Không tìm thấy bài tập."));
        }

        if (string.IsNullOrWhiteSpace(request.StudentCode))
        {
            return (IResult)Results.BadRequest(new ApiError("STUDENT_CODE_REQUIRED", "Mã sinh viên không được để trống."));
        }

        if (!Uri.TryCreate(request.RepositoryUrl, UriKind.Absolute, out var repositoryUri) ||
            (repositoryUri.Scheme != Uri.UriSchemeHttp && repositoryUri.Scheme != Uri.UriSchemeHttps))
        {
            return (IResult)Results.BadRequest(new ApiError("INVALID_REPOSITORY_URL", "RepositoryUrl phải là URL HTTP hoặc HTTPS hợp lệ."));
        }

        var now = DateTimeOffset.UtcNow;
        var submission = new SubmissionDto(
            Guid.NewGuid(), assignmentId, request.StudentCode.Trim(), repositoryUri.ToString(),
            "received", now, now);

        store.AddSubmission(submission);

        // Mô phỏng hệ thống chấm bài chạy sau request REST; SignalR chỉ báo các thay đổi trạng thái.
        var shouldFail = repositoryUri.AbsolutePath.Contains("/fail", StringComparison.OrdinalIgnoreCase);
        _ = SimulateGradingAsync(submission.Id, store, hubContext, shouldFail);

        return (IResult)Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapHub<SubmissionHub>("/hubs/submissions");
app.MapHub<PingHub>("/hubs/ping");

app.Run();

static async Task SimulateGradingAsync(
    Guid submissionId,
    SubmissionStore store,
    IHubContext<SubmissionHub> hubContext,
    bool shouldFail)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    await PublishStatusAsync(submissionId, "processing", store, hubContext);

    await Task.Delay(TimeSpan.FromSeconds(2));
    await PublishStatusAsync(submissionId, shouldFail ? "failed" : "graded", store, hubContext);
}

static async Task PublishStatusAsync(
    Guid submissionId,
    string status,
    SubmissionStore store,
    IHubContext<SubmissionHub> hubContext)
{
    var updated = store.UpdateStatus(submissionId, status, DateTimeOffset.UtcNow);
    if (updated is null)
    {
        return;
    }

    var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);
    await hubContext.Clients
        .Group(SubmissionGroups.ForSubmission(submissionId))
        .SendAsync("SubmissionStatusChanged", message);
}

public sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, SubmissionGroups.ForSubmission(submissionId));
}

// Hub nhỏ dùng trong phần đầu bài giảng để minh họa Echo, broadcast và group.
public sealed class PingHub : Hub
{
    public Task Echo(string message) =>
        Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");

    public Task Broadcast(string message) =>
        Clients.All.SendAsync("BroadcastMessage", message);

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync(
            "GroupMessage", $"Client {Context.ConnectionId[..8]} đã tham gia nhóm {groupName}.");
    }

    public Task SendToGroup(string groupName, string message) =>
        Clients.Group(groupName).SendAsync("GroupMessage", message);

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] + {Context.ConnectionId[..8]} kết nối");
        await Clients.All.SendAsync("SystemMessage", $"Client {Context.ConnectionId[..8]} đã kết nối.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] - {Context.ConnectionId[..8]} ngắt kết nối");
        await Clients.All.SendAsync("SystemMessage", $"Client {Context.ConnectionId[..8]} đã ngắt kết nối.");
        await base.OnDisconnectedAsync(exception);
    }
}

public static class SubmissionGroups
{
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

public sealed class SubmissionStore
{
    private readonly object _gate = new();
    private readonly List<AssignmentDto> _assignments =
    [
        new(1, "Bài tập SignalR", "Tạo repository và theo dõi trạng thái chấm bài theo thời gian thực.")
    ];
    private readonly Dictionary<Guid, SubmissionDto> _submissions = new();

    public IReadOnlyList<AssignmentDto> GetAssignments() => _assignments;

    public AssignmentDto? FindAssignment(int id) => _assignments.FirstOrDefault(item => item.Id == id);

    public SubmissionDto? FindSubmission(Guid id)
    {
        lock (_gate)
        {
            return _submissions.GetValueOrDefault(id);
        }
    }

    public void AddSubmission(SubmissionDto submission)
    {
        lock (_gate)
        {
            _submissions.Add(submission.Id, submission);
        }
    }

    public SubmissionDto? UpdateStatus(Guid id, string status, DateTimeOffset updatedAt)
    {
        lock (_gate)
        {
            if (!_submissions.TryGetValue(id, out var current))
            {
                return null;
            }

            var updated = current with { Status = status, UpdatedAt = updatedAt };
            _submissions[id] = updated;
            return updated;
        }
    }
}

public sealed record AssignmentDto(int Id, string Title, string Description);
public sealed record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
public sealed record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
public sealed record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
public sealed record ApiError(string Code, string Message);
