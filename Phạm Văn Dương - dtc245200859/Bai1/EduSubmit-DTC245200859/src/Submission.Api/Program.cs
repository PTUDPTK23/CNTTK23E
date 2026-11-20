using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Giao tiep Client-API")
};

// Luu tru danh sach bai nop trong bo nho
var submissions = new ConcurrentDictionary<Guid, SubmissionResult>();

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapPost("/api/submissions", (CreateSubmissionRequest request) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.StudentCode) || request.AssignmentId <= 0)
    {
        return Results.BadRequest(new { message = "Thong tin bai nop khong hop le." });
    }

    var result = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode,
        "received",
        DateTimeOffset.UtcNow
    );

    submissions[result.Id] = result;

    return Results.Created($"/api/submissions/{result.Id}", result);
});

// BƯỚC 12: API tra cứu lượt nộp bài theo ID
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    if (submissions.TryGetValue(id, out var submission))
    {
        return Results.Ok(submission);
    }
    return Results.NotFound(new { message = $"Khong tim thay luot nop bai voi ID: {id}" });
});

app.Run();

record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);