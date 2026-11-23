using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

// Danh sach bai tap
var assignments = new List<AssignmentDto>
{
    new(1, "Bai 3 - SignalR", true),
    new(2, "Bai tap da dong", false)
};

// Danh sach luot nop va khoa du lieu
var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

// Kiem tra may chu
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.RealtimeApi",
    status = "running"
}));

// Lay danh sach bai tap
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// Tao luot nop bai
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

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
            "Bai tap da dong."));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Thieu ma sinh vien hoac URL repository."));
    }

    var now = DateTimeOffset.UtcNow;

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode,
        request.RepositoryUrl,
        "received",
        now,
        now
    );

    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Buoc 4: Khoi dong cham bai nen
    _ = SimulateGradingAsync(
        submission.Id,
        submissions,
        submissionsLock,
        hubContext);

    return Results.Created(
        $"/api/submissions/{submission.Id}", submission);
});

// Doc thong tin luot nop
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            s => s.Id == submissionId);

        return submission is null
            ? Results.NotFound(new ApiError(
                "SUBMISSION_NOT_FOUND",
                "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// Buoc 4: Mo phong qua trinh cham bai
static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext)
{
    await Task.Delay(1000);

    await UpdateStatusAsync(
        id, "processing",
        submissions, submissionsLock, hubContext);

    await Task.Delay(2000);

    string finalStatus;

    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(s => s.Id == id);

        finalStatus = submission?.RepositoryUrl.StartsWith(
            "https://", StringComparison.OrdinalIgnoreCase) == true
                ? "graded"
                : "failed";
    }

    await UpdateStatusAsync(
        id, finalStatus,
        submissions, submissionsLock, hubContext);
}

// Cap nhat trang thai va gui thong bao SignalR
static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> submissions,
    object submissionsLock,
    IHubContext<SubmissionHub> hubContext)
{
    SubmissionDto? updated = null;

    lock (submissionsLock)
    {
        int index = submissions.FindIndex(s => s.Id == id);

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
        .Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", message);

    Console.WriteLine($"[Push] {id} -> {status}");
}

// Cac DTO
record AssignmentDto(
    int Id,
    string Title,
    bool IsOpen);

record CreateSubmissionRequest(
    string? StudentCode,
    string? RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);

// Du lieu thong bao SignalR
record SubmissionStatusMessage(
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAt);

// SubmissionHub
sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        string groupName =
            SubmissionGroups.ForSubmission(submissionId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId, groupName);

        Console.WriteLine(
            $"[Hub] {Context.ConnectionId} theo doi {groupName}");
    }
}
// Tao nhom rieng cho tung luot nop
static class SubmissionGroups
{
    public static string ForSubmission(Guid id)
    {
        return ForSubmission(id.ToString());
    }

    public static string ForSubmission(string id)
    {
        return $"submission:{id}";
    }
}
