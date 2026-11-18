using Microsoft.AspNetCore.SignalR;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();


var assignments = new List<AssignmentDto> {
    new(1, "Bai tap REST API", "Thiet ke endpoint va DTO theo chuan REST", true),
    new(2, "Bai tap Client", "Viet console client voi HttpClient", true),
    new(3, "Bai tap Cu - Da dong", "Bai tap mau da qua han nop", false)
};

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/", () => Results.Ok(new {
    service = "Submission.Api",
    status = "running"
}));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

IResult HandleSubmission(
    int assignmentId,
    string? studentCode,
    string? repositoryUrl,
    IHubContext<SubmissionHub> hubContext)
{if (string.IsNullOrWhiteSpace(studentCode) || string.IsNullOrWhiteSpace(repositoryUrl))
    return Results.BadRequest(new ApiError(
        "INVALID_SUBMISSION",
        "Thieu ma sinh vien hoac kho ma nguon."));

var validStudentCode = studentCode!.Trim();
var validRepositoryUrl = repositoryUrl!.Trim();

SubmissionDto submission;

lock (submissionsLock)
{
    var duplicated = submissions.Any(item =>
        item.AssignmentId == assignmentId &&
        item.StudentCode.Equals(validStudentCode, StringComparison.OrdinalIgnoreCase));

    if (duplicated)
        return Results.Conflict(new ApiError(
            "DUPLICATE_SUBMISSION",
            "Sinh vien da nop bai tap nay."));

    submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: validStudentCode,
        RepositoryUrl: validRepositoryUrl,
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow);

    submissions.Add(submission);
}
_ = SimulateGradingAsync(
    submission.Id, submissions, submissionsLock, hubContext);

return Results.Created($"/api/submissions/{submission.Id}", submission);
}

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>
{
    return HandleSubmission(
        assignmentId, request.StudentCode,
        request.RepositoryUrl, hubContext);
});

app.MapPost("/api/submissions",
    (GeneralSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>
{
    if (request.AssignmentId <= 0)
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION", "Ma bai tap khong hop le."));

    return HandleSubmission(
        request.AssignmentId, request.StudentCode,
        request.RepositoryUrl, hubContext);
});

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
            : Results.Ok(submission);
    }
});

app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        if (submission is null)
            return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop de huy."));

        submissions.Remove(submission);
        return Results.NoContent();
    }
});
app.MapHub<SubmissionHub>("/hubs/submissions");
app.Run();

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

    var message = new SubmissionStatusMessage(
        updated.Id,
        updated.Status,
        updated.UpdatedAt);

    await hub.Clients.Group(SubmissionGroups.ForSubmission(id))
        .SendAsync("SubmissionStatusChanged", message);
}
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record GeneralSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(string submissionId)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups
{
    public static string ForSubmission(Guid id)
        => ForSubmission(id.ToString());

    public static string ForSubmission(string id)
        => $"submission:{id}";
}
record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);