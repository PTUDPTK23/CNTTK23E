using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

// Du lieu mau
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint...", true),
    new(2, "Bai tap Client", "Viet console client...", false),
    new(3, "Bai tap mo rong", "Bai tap da dong cho ca 9a.", false)
};

var submissions = new SubmissionStore();

// GET / - Kiem tra may chu
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// GET - Danh sach bai tap
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// GET - Chi tiet bai tap
app.MapGet("/api/assignments/{assignmentId:int}",
    (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    return Results.Ok(assignment);
});

// POST - Nop bai
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hub, ILogger<SubmissionHub> logger) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    // Bai tap khong ton tai
    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    // Bai tap da dong
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED",
            "Bai tap da dong."));
    }

    // Kiem tra du lieu
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Ma sinh vien va link bai nop khong duoc de trong."));
    }

    var submission = submissions.Create(
        assignmentId, request.StudentCode.Trim(), request.RepositoryUrl.Trim());

    if (submission is null)
    {
        return Results.Conflict(new ApiError(
            "DUPLICATE_SUBMISSION",
            "Sinh vien da nop bai tap nay."));
    }

    _ = SimulateGradingAsync(submission.Id, hub, logger);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// GET - Doc lai bai nop
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    var submission = submissions.Find(submissionId);

    if (submission is null)
    {
        return Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay bai nop."));
    }

    return Results.Ok(submission);
});

// DELETE - Rut bai nop
app.MapDelete("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    if (!submissions.Remove(submissionId))
    {
        return Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay bai nop."));
    }

    return Results.NoContent();
});

app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

async Task SimulateGradingAsync(Guid id, IHubContext<SubmissionHub> hub,
    ILogger<SubmissionHub> logger)
{
    var stopping = app.Lifetime.ApplicationStopping;

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(1), stopping);
        await UpdateStatusAsync(id, "processing", hub, logger, stopping);
        await Task.Delay(TimeSpan.FromSeconds(2), stopping);
        await UpdateStatusAsync(id, "graded", hub, logger, stopping);
    }
    catch (OperationCanceledException) when (stopping.IsCancellationRequested)
    {
        // May chu dang dung.
    }
}

async Task UpdateStatusAsync(Guid id, string status, IHubContext<SubmissionHub> hub,
    ILogger<SubmissionHub> logger, CancellationToken stopping)
{
    var updated = submissions.UpdateStatus(id, status);
    if (updated is null)
        return;

    var group = SubmissionHub.GroupName(id);
    var message = new SubmissionStatusMessage(updated.Id, updated.Status, updated.UpdatedAt);

    try
    {
        await hub.Clients.Group(group).SendAsync("SubmissionStatusChanged", message, stopping);
        logger.LogInformation("[Push] {Group} -> {Status}", group, status);
    }
    catch (Exception ex) when (!stopping.IsCancellationRequested)
    {
        logger.LogWarning(ex, "Loi gui SignalR cho {Group}, trang thai REST da duoc luu", group);
    }
}

// DTO
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
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);
