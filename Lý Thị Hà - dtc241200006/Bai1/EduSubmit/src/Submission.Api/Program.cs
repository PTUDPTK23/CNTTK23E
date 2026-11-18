var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Danh sách lưu trữ tạm thời các lượt nộp bài trong bộ nhớ RAM
var submissions = new List<SubmissionResult>();

var assignments = new[] {
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Xay dung Minimal API nang cao")
};

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Endpoint POST: Nhận bài nộp và lưu vào danh sách
app.MapPost("/api/submissions", (CreateSubmissionRequest request) => {
    if (request.AssignmentId <= 0 || string.IsNullOrEmpty(request.StudentCode)) {
        return Results.BadRequest(new { message = "Du lieu khong hop le hoac thieu AssignmentId/StudentCode." });
    }

    var result = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode,
        "received",
        DateTimeOffset.UtcNow
    );

    // Lưu lượt nộp vào danh sách
    submissions.Add(result);

    return Results.Created($"/api/submissions/{result.Id}", result);
});

// [BỔ SUNG BƯỚC 12] Endpoint GET theo ID (Guid) để đọc lại lượt nộp
app.MapGet("/api/submissions/{id:guid}", (Guid id) => {
    var submission = submissions.FirstOrDefault(s => s.Id == id);
    if (submission == null) {
        return Results.NotFound(new { message = $"Khong tim thay luot nop voi id: {id}" });
    }
    return Results.Ok(submission);
});

app.Run();

record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);