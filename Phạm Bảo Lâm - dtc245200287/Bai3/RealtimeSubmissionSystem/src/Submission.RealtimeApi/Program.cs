var builder = WebApplication.CreateBuilder(args);

// 👇 ĐĂNG KÝ DỊCH VỤ SIGNALR
builder.Services.AddSignalR();

var app = builder.Build();

var submissions = new List<SubmissionDto>();
var submissionsLock = new object();

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    lock (submissionsLock) {
        var submission = submissions.FirstOrDefault(i => i.Id == submissionId);
        if (submission is null)
            return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Không tìm thấy submission"));
        return Results.Ok(submission);
    }
});

app.MapPost("/api/submissions", (SubmissionDto dto) => {
    lock (submissionsLock) {
        submissions.Add(dto);
        return Results.Created($"/api/submissions/{dto.Id}", dto);
    }
});

// 👇 MAP HUB ĐƯỜNG DẪN ENDPOINT REALTIME
app.MapHub<SubmissionHub>("/hubs/submission");

app.Run();

record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);