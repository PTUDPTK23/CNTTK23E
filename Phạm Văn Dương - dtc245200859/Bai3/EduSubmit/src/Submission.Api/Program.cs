using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<SubmissionStore>();
builder.Services.AddSingleton<SubmissionStatusWorker>();
builder.Services.AddHostedService(static services =>
    services.GetRequiredService<SubmissionStatusWorker>());

var app = builder.Build();

var assignments = new[]
{
    new AssignmentDto(1, "Bai tap REST API", "Thiet ke API dau tien", true),
    new AssignmentDto(2, "Bai tap Client", "Goi API tu client", true),
    new AssignmentDto(3, "Bai tap HTTP", "Thuc hanh request response", true)
};

app.MapGet("/", () => Results.Ok(new
{
    service = "EduSubmit.Realtime.Api",
    status = "running"
}));

// REST cua Bai 2 duoc giu nguyen: danh sach va chi tiet bai tap.
app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);

    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// POST van tao submission qua REST; xu ly trang thai duoc day them qua SignalR.
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    async (int assignmentId,
        CreateSubmissionRequest? request,
        SubmissionStore store,
        SubmissionStatusWorker statusWorker,
        CancellationToken cancellationToken) =>
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);

        if (assignment is null)
        {
            return Results.NotFound(new ApiError(
                "ASSIGNMENT_NOT_FOUND",
                "Khong tim thay bai tap."));
        }

        if (!assignment.IsOpen)
        {
            return Results.BadRequest(new ApiError(
                "ASSIGNMENT_CLOSED",
                "Bai tap da dong, khong the nop bai."));
        }

        if (request is null ||
            string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError(
                "INVALID_SUBMISSION",
                "Ma sinh vien va dia chi repository khong duoc de trong."));
        }

        var submission = store.Create(
            assignmentId,
            request.StudentCode.Trim(),
            request.RepositoryUrl.Trim());

        await statusWorker.QueueAsync(submission.Id, cancellationToken);

        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// GET van doc lai submission theo Location cua POST.
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId, SubmissionStore store) =>
    {
        var submission = store.Get(submissionId);

        return submission is null
            ? Results.NotFound(new ApiError(
                "SUBMISSION_NOT_FOUND",
                "Khong tim thay luot nop."))
            : Results.Ok(submission);
    });

// Kenh SignalR rieng cho thong bao trang thai submission.
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static class SubmissionStatuses
{
    public const string Received = "received";
    public const string Processing = "processing";
    public const string Graded = "graded";
}

static class SubmissionGroups
{
    public static string For(Guid submissionId) => $"submission:{submissionId}";
}

sealed class SubmissionStore
{
    private readonly ConcurrentDictionary<Guid, SubmissionDto> submissions = new();

    public SubmissionDto Create(int assignmentId, string studentCode, string repositoryUrl)
    {
        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            studentCode,
            repositoryUrl,
            SubmissionStatuses.Received,
            DateTimeOffset.UtcNow);

        submissions[submission.Id] = submission;
        return submission;
    }

    public SubmissionDto? Get(Guid submissionId)
        => submissions.TryGetValue(submissionId, out var submission)
            ? submission
            : null;

    public SubmissionDto? UpdateStatus(Guid submissionId, string status)
    {
        while (submissions.TryGetValue(submissionId, out var current))
        {
            var updated = current with { Status = status };

            if (submissions.TryUpdate(submissionId, updated, current))
            {
                return updated;
            }
        }

        return null;
    }
}

sealed class SubmissionStatusWorker : BackgroundService
{
    private readonly Channel<Guid> pendingSubmissions =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    private readonly SubmissionStore store;
    private readonly IHubContext<SubmissionHub> hubContext;
    private readonly ILogger<SubmissionStatusWorker> logger;

    public SubmissionStatusWorker(
        SubmissionStore store,
        IHubContext<SubmissionHub> hubContext,
        ILogger<SubmissionStatusWorker> logger)
    {
        this.store = store;
        this.hubContext = hubContext;
        this.logger = logger;
    }

    public ValueTask QueueAsync(Guid submissionId, CancellationToken cancellationToken)
        => pendingSubmissions.Writer.WriteAsync(submissionId, cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var submissionId in pendingSubmissions.Reader
                           .ReadAllAsync(stoppingToken))
        {
            try
            {
                // Cho client co thoi gian vao group sau khi nhan 201 Created.
                await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                await PublishStatusAsync(submissionId, SubmissionStatuses.Processing);

                await Task.Delay(TimeSpan.FromMilliseconds(1500), stoppingToken);
                await PublishStatusAsync(submissionId, SubmissionStatuses.Graded);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Khong the cap nhat trang thai submission {SubmissionId}",
                    submissionId);
            }
        }
    }

    private async Task PublishStatusAsync(Guid submissionId, string status)
    {
        var submission = store.UpdateStatus(submissionId, status);
        if (submission is null)
        {
            return;
        }

        var message = new SubmissionStatusMessage(
            submission.Id,
            submission.Status,
            DateTimeOffset.UtcNow);

        await hubContext.Clients
            .Group(SubmissionGroups.For(submissionId))
            .SendAsync("SubmissionStatusChanged", message);

        logger.LogInformation(
            "Submission {SubmissionId} changed to {Status}",
            submissionId,
            status);
    }
}

sealed class SubmissionHub : Hub
{
    private readonly SubmissionStore store;

    public SubmissionHub(SubmissionStore store)
    {
        this.store = store;
    }

    public async Task WatchSubmission(Guid submissionId)
    {
        if (store.Get(submissionId) is null)
        {
            throw new HubException("Khong tim thay submission.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.For(submissionId));

        // Gui lai trang thai hien tai de client khong phu thuoc vao thoi diem join.
        var submission = store.Get(submissionId);
        if (submission is null)
        {
            return;
        }

        await Clients.Caller.SendAsync(
            "SubmissionStatusChanged",
            new SubmissionStatusMessage(
                submission.Id,
                submission.Status,
                DateTimeOffset.UtcNow));
    }

    public Task LeaveSubmission(Guid submissionId)
        => Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.For(submissionId));
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);
