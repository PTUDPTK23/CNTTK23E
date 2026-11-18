
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dang ky dich vu SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Danh sach bai tap
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Lam quen voi ung dung phan tan"),
    new Assignment(3, "Bai tap 3", "Xay dung ung dung thoi gian thuc voi SignalR")
};

// Danh sach bai nop
var submissions = new List<SubmissionDto>();

// Khoa dong bo khi truy cap danh sach bai nop
var submissionsLock = new object();

// Trang chu
app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        application = "EduSubmit Realtime API",
        message = "Realtime Submission API is running."
    });
});

// 1. Lay danh sach bai tap
app.MapGet("/api/assignments", () =>
{
    return Results.Ok(assignments);
});

// 2. Gui bai nop
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest request,
     IHubContext<SubmissionHub> hubContext) =>
{
    // Kiem tra bai tap co ton tai khong
    var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);

    if (assignment == null)
    {
        return Results.NotFound(new
        {
            message = "Khong tim thay bai tap."
        });
    }

    // Kiem tra ma sinh vien
    if (string.IsNullOrWhiteSpace(request.StudentCode))
    {
        return Results.BadRequest(new
        {
            message = "StudentCode khong duoc de trong."
        });
    }

    // Kiem tra duong dan repository
    if (string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new
        {
            message = "RepositoryUrl khong duoc de trong."
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
        now
    );

    // Luu bai nop truoc khi tra ve HTTP 201
    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    // Xu ly cham bai nen, khong doi ket qua cham bai
    _ = SimulateGradingAsync(submission.Id, hubContext);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission
    );
});

// 3. Tra cuu bai nop theo ID
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    SubmissionDto? submission;

    lock (submissionsLock)
    {
        submission = submissions.FirstOrDefault(s => s.Id == id);
    }

    if (submission == null)
    {
        return Results.NotFound(new
        {
            message = "Khong tim thay bai nop."
        });
    }

    return Results.Ok(submission);
});

// 4. Xem tat ca bai nop
app.MapGet("/api/submissions", () =>
{
    lock (submissionsLock)
    {
        return Results.Ok(submissions.ToList());
    }
});

// 5. SignalR Hub
app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();

// =========================
// HAM GIA LAP QUA TRINH CHAM BAI
// =========================

async Task SimulateGradingAsync(
    Guid submissionId,
    IHubContext<SubmissionHub> hubContext)
{
    // Sau 1 giay: chuyen sang processing
    await Task.Delay(1000);

    await UpdateStatusAsync(
        submissionId,
        "processing",
        hubContext
    );

    // Sau them 2 giay: chuyen sang graded
    await Task.Delay(2000);

    await UpdateStatusAsync(
        submissionId,
        "graded",
        hubContext
    );
}

// Cap nhat trang thai va gui thong bao
async Task UpdateStatusAsync(
    Guid submissionId,
    string newStatus,
    IHubContext<SubmissionHub> hubContext)
{
    SubmissionDto? updatedSubmission;

    lock (submissionsLock)
    {
        int index = submissions.FindIndex(
            s => s.Id == submissionId
        );

        if (index == -1)
        {
            return;
        }

        updatedSubmission = submissions[index] with
        {
            Status = newStatus,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Luu trang thai moi truoc khi gui thong bao
        submissions[index] = updatedSubmission;
    }

    var message = new SubmissionStatusMessage(
        updatedSubmission.Id,
        updatedSubmission.Status,
        updatedSubmission.UpdatedAt
    );

    // Chi gui thong bao den client dang theo doi bai nop nay
    await hubContext.Clients
        .Group(SubmissionGroups.ForSubmission(submissionId))
        .SendAsync("SubmissionStatusChanged", message);
}

// =========================
// CAC KIEU DU LIEU
// =========================

record Assignment(
    int Id,
    string Title,
    string Description
);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
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

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt
);

// =========================
// SIGNALR HUB
// =========================

class SubmissionHub : Hub
{
    public async Task WatchSubmission(Guid submissionId)
    {
        string groupName =
            SubmissionGroups.ForSubmission(submissionId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName
        );
    }
}

// Tao ten nhom rieng cho tung bai nop
static class SubmissionGroups
{
    public static string ForSubmission(Guid submissionId)
    {
        return $"submission:{submissionId}";
    }
}
