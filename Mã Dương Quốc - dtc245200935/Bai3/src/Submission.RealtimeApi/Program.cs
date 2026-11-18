using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(
    new { service = "Submission.RealtimeApi", status = "running" }));

app.MapGet("/api/assignments", () =>
    Results.Ok(new[]
    {
        new AssignmentDto(201, "Realtime Submission System")
    }));

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/api/submissions", () =>
{
    lock (submissionsLock)
    {
        return Results.Ok(submissions.ToList());
    }
});

app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            item => item.Id == id);

        return submission is null
            ? Results.NotFound(new { message = "SUBMISSION_NOT_FOUND" })
            : Results.Ok(submission);
    }
});

app.MapPost("/api/submissions", (CreateSubmissionDto dto) =>
{
    if (dto.AssignmentId <= 0 ||
        string.IsNullOrWhiteSpace(dto.StudentCode) ||
        string.IsNullOrWhiteSpace(dto.RepositoryUrl))
    {
        return Results.BadRequest(new
        {
            message = "INVALID_SUBMISSION",
            errors = new[]
            {
                "AssignmentId phai lon hon 0.",
                "StudentCode khong duoc de trong.",
                "RepositoryUrl khong duoc de trong."
            }
        });
    }

    var now = DateTimeOffset.UtcNow;
    var submission = new SubmissionDto(
        Guid.NewGuid(),
        dto.AssignmentId,
        dto.StudentCode.Trim(),
        dto.RepositoryUrl.Trim(),
        "received",
        now,
        now);

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionDto request,
        IHubContext<SubmissionHub> hubContext) =>
    {
        if (assignmentId <= 0 ||
            string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new
            {
                message = "INVALID_SUBMISSION",
                errors = new[]
                {
                    "AssignmentId phai lon hon 0.",
                    "StudentCode khong duoc de trong.",
                    "RepositoryUrl khong duoc de trong."
                }
            });
        }

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

        _ = SimulateGradingAsync(
            submission.Id,
            submissions,
            submissionsLock,
            hubContext);

        return Results.Created(
            $"/api/submissions/{submission.Id}",
            submission);
    });

app.MapHub<RealtimeHub>("/hubs/realtime");
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

static async Task SimulateGradingAsync(
    Guid submissionId,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext)
{
    await Task.Delay(400);
    await UpdateStatusAsync(
        submissionId,
        "processing",
        submissions,
        submissionsLock,
        hubContext);

    await Task.Delay(400);
    await UpdateStatusAsync(
        submissionId,
        "graded",
        submissions,
        submissionsLock,
        hubContext);
}

static async Task UpdateStatusAsync(
    Guid submissionId,
    string status,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext
)
{
    SubmissionDto? updated = null;
    lock (submissionsLock)
    {
        var index = submissions.FindIndex(
            item => item.Id == submissionId);

        if (index >= 0)
        {
            updated = submissions[index] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            submissions[index] = updated;
        }
    }

    if (updated is null)
    {
        return;
    }

    var message = new SubmissionStatusMessage(
        updated.Id,
        updated.Status,
        updated.UpdatedAt);

    await hubContext.Clients
        .Group(SubmissionGroups.ForSubmission(
            submissionId.ToString()))
        .SendAsync("SubmissionStatusChanged", message);
}

record CreateSubmissionDto(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAt);

record AssignmentDto(
    int Id,
    string Title);

sealed class RealtimeHub : Hub
{
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("GroupMessage",
            $"{GetShortConnectionId()} da tham gia nhom {groupName}");
    }

    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("GroupMessage", message);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] + {GetShortConnectionId()} ket noi");
        await Clients.All.SendAsync("SystemMessage",
            $"Client {GetShortConnectionId()} da ket noi.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] - {GetShortConnectionId()} ngat");
        await Clients.All.SendAsync("SystemMessage",
            $"Client {GetShortConnectionId()} da ngat ket noi.");
        await base.OnDisconnectedAsync(exception);
    }

    private string GetShortConnectionId()
    {
        return Context.ConnectionId.Length <= 8
            ? Context.ConnectionId
            : Context.ConnectionId[..8];
    }
}

sealed class SubmissionHub : Hub
{
    public async Task MatchSubmission(string submissionId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(string id) => $"submission:{id}";
}
