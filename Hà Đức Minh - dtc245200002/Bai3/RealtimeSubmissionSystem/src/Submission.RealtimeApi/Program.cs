
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Dang ky dich vu SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Du lieu bai tap mau
var assignments = new List<AssignmentDto>
{
    new AssignmentDto(
        1,
        "Bai tap SignalR",
        "Xay dung ung dung nop bai va cap nhat trang thai theo thoi gian thuc",
        true
    )
};

// Danh sach luot nop bai
var submissions = new List<SubmissionDto>();

// Khoa bao ve du lieu khi HTTP va tac vu nen cung truy cap
var submissionsLock = new object();

// =====================================================
// 1. REST API
// =====================================================

// Kiem tra may chu
app.MapGet("/", () => "Realtime Submission API is running!");

// Lay danh sach bai tap
app.MapGet("/api/assignments", () =>
{
    return Results.Ok(assignments);
});

// Tao luot nop bai
app.MapPost(
    "/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId,
     CreateSubmissionRequest? request,
     IHubContext<SubmissionHub> hubContext) =>
{
    // Kiem tra bai tap co ton tai khong
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId
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

    // Kiem tra bai tap co dang mo khong
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(
            new ApiError(
                "ASSIGNMENT_CLOSED",
                "Bai tap da dong."
            )
        );
    }

    // Kiem tra du lieu dau vao
    if (request is null ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(
            new ApiError(
                "INVALID_SUBMISSION",
                "Ma sinh vien va RepositoryUrl khong duoc de trong."
            )
        );
    }

    // Tao luot nop bai
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

    // Them vao danh sach co khoa bao ve
    lock (submissionsLock)
    {
        submissions.Add(submission);
    }

    Console.WriteLine(
        $"[Submission] Tao luot nop {submission.Id} - {submission.StudentCode}"
    );

    // Chay tac vu cham bai nen, khong doi ket qua
    _ = SimulateGradingAsync(
        submission.Id,
        submissions,
        submissionsLock,
        hubContext
    );

    // Tra ve HTTP 201 Created ngay lap tuc
    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission
    );
});

// Lay thong tin mot luot nop bai theo ID
app.MapGet(
    "/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            s => s.Id == submissionId
        );

        if (submission is null)
        {
            return Results.NotFound(
                new ApiError(
                    "SUBMISSION_NOT_FOUND",
                    "Khong tim thay luot nop bai."
                )
            );
        }

        return Results.Ok(submission);
    }
});

// =====================================================
// 2. SIGNALR HUB
// =====================================================

// Dang ky Hub theo duong dan /hubs/submissions
app.MapHub<SubmissionHub>("/hubs/submissions");

// Chay may chu tai cong 5000
app.Run("http://localhost:5000");

// =====================================================
// 3. TAC VU NEN GIA LAP CHAM BAI
// =====================================================

static async Task SimulateGradingAsync(
    Guid id,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    // Sau 1 giay: received -> processing
    await Task.Delay(TimeSpan.FromSeconds(1));

    await UpdateStatusAsync(
        id,
        "processing",
        list,
        gate,
        hub
    );

    // Sau them 2 giay: processing -> graded
    await Task.Delay(TimeSpan.FromSeconds(2));

    await UpdateStatusAsync(
        id,
        "graded",
        list,
        gate,
        hub
    );
}

// Cap nhat trang thai va gui thong bao SignalR
static async Task UpdateStatusAsync(
    Guid id,
    string status,
    List<SubmissionDto> list,
    object gate,
    IHubContext<SubmissionHub> hub)
{
    SubmissionDto? updated = null;

    // Buoc 1: Cap nhat du lieu truoc
    lock (gate)
    {
        var index = list.FindIndex(
            s => s.Id == id
        );

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

    if (updated is null)
    {
        return;
    }

    // Buoc 2: Tao thong diep toi gian
    var message = new SubmissionStatusMessage(
        updated.Id,
        updated.Status,
        updated.UpdatedAt
    );

    // Buoc 3: Gui den dung nhom dang theo doi
    var groupName = SubmissionGroups.ForSubmission(id);

    await hub.Clients
        .Group(groupName)
        .SendAsync("SubmissionStatusChanged", message);

    Console.WriteLine(
        $"[Push] {groupName} -> {status}"
    );
}

// =====================================================
// 4. SIGNALR HUB CLASS
// =====================================================

public sealed class SubmissionHub : Hub
{
    // May khach goi sau khi da co ID luot nop
    public Task WatchSubmission(string submissionId)
    {
        var groupName = SubmissionGroups.ForSubmission(
            submissionId
        );

        Console.WriteLine(
            $"[Hub] {Context.ConnectionId[..8]} theo doi {groupName}"
        );

        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName
        );
    }
}

// =====================================================
// 5. TAO TEN GROUP DUNG CHUNG
// =====================================================

public static class SubmissionGroups
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

// =====================================================
// 6. CAC RECORD DU LIEU
// =====================================================

public record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen
);

public record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
);

public record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);

public record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt
);

public record ApiError(
    string Code,
    string Message
);
