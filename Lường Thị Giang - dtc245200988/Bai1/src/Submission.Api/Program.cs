
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Danh sach bai tap
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tim hieu REST API"),
    new Assignment(2, "Bai tap 2", "Ket noi Client va Server")
};

// Luu bai nop trong bo nho
var submissions = new Dictionary<Guid, SubmissionResult>();

// Kiem tra trang thai API
app.MapGet("/", () => Results.Ok(new
{
    service = "EduSubmit API - Luong Thi Giang",
    studentCode = "DTC245200988",
    status = "running"
}));

// Lay danh sach bai tap
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// Nop bai
app.MapPost("/api/submissions", (CreateSubmissionRequest request) =>
{
    if (!assignments.Any(a => a.Id == request.AssignmentId) ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new
        {
            message = "Thong tin nop bai khong hop le"
        });
    }

    var submission = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode,
        "received",
        DateTimeOffset.UtcNow
    );

    submissions[submission.Id] = submission;

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// Tra cuu bai nop
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    if (submissions.TryGetValue(id, out var submission))
        return Results.Ok(submission);

    return Results.NotFound(new
    {
        message = "Khong tim thay bai nop"
    });
});

app.Run();

record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt);
