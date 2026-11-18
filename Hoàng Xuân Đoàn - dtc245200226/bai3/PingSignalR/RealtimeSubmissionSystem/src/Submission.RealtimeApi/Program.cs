using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

var assignments = new List<AssignmentDto> {
    new AssignmentDto(1, "Bài tập SignalR cơ bản", "Mô tả bài tập thực hành SignalR")
};

// Endpoint lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Endpoint GET theo ID lượt nộp
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock) {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        return submission is null
            ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Không tìm thấy lượt nộp"))
            : Results.Ok(submission);
    }
});

// Endpoint tạo lượt nộp mới - POST (Tích hợp phát thông báo Realtime - Bước 5)
app.MapPost("/api/assignments/{assignmentId:int}/submissions", async (int assignmentId, CreateSubmissionRequest request, IHubContext<SubmissionHub> hubContext) => {
    var assignmentExists = assignments.Any(a => a.Id == assignmentId);
    if (!assignmentExists) {
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Không tìm thấy bài tập"));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) || string.IsNullOrWhiteSpace(request.RepositoryUrl)) {
        return Results.BadRequest(new ApiError("INVALID_REQUEST", "Thiếu thông tin sinh viên hoặc link git"));
    }

    var submission = new SubmissionDto(
        Id: Guid.NewGuid(),
        AssignmentId: assignmentId,
        StudentCode: request.StudentCode,
        RepositoryUrl: request.RepositoryUrl,
        Status: "received",
        ReceivedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow
    );

    lock (submissionsLock) {
        submissions.Add(submission);
    }

    await PublishStatusAsync(hubContext, submission);
    _ = SimulateGradingAsync(submission, hubContext);

    return Results.Created($"/api/submissions/{submission.Id}", submission);
});

// Ánh xạ Hub SignalR
app.MapHub<SubmissionHub>("/hubs/submissions");

async Task PublishStatusAsync(IHubContext<SubmissionHub> hubContext, SubmissionDto submission) {
    var groupName = SubmissionGroups.ForSubmission(submission.Id);
    var message = new SubmissionStatusMessage(submission.Id, submission.Status, submission.UpdatedAt);

    await hubContext.Clients.Group(groupName).SendAsync("SubmissionStatusChanged", message);
}

SubmissionDto UpdateStatus(SubmissionDto submission, string status) {
    var updated = submission with {
        Status = status,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    lock (submissionsLock) {
        var index = submissions.FindIndex(i => i.Id == submission.Id);
        if (index >= 0) {
            submissions[index] = updated;
        }
    }

    return updated;
}

async Task SimulateGradingAsync(SubmissionDto submission, IHubContext<SubmissionHub> hubContext) {
    await Task.Delay(TimeSpan.FromSeconds(1));

    SubmissionDto updated;
    if (!submission.RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
        updated = UpdateStatus(submission, "failed");
        await PublishStatusAsync(hubContext, updated);
        return;
    }

    updated = UpdateStatus(submission, "processing");
    await PublishStatusAsync(hubContext, updated);

    await Task.Delay(TimeSpan.FromSeconds(2));

    updated = UpdateStatus(updated, "graded");
    await PublishStatusAsync(hubContext, updated);
}

app.Run();

sealed class SubmissionHub : Hub {
    public Task WatchSubmission(string submissionId) {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            SubmissionGroups.ForSubmission(submissionId));
    }
}

static class SubmissionGroups {
    public static string ForSubmission(Guid id) => ForSubmission(id.ToString());
    public static string ForSubmission(string id) => $"submission:{id}";
}

sealed record AssignmentDto(int Id, string Title, string Description);

sealed record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

sealed record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

sealed record ApiError(string Code, string Message);

sealed record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);