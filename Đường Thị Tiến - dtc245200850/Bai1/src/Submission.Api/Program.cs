var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Goi API tu client")
};

// 1. Tạo danh sách bộ nhớ lưu các lượt nộp
var submissions = new List<SubmissionResult>();

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapPost("/api/submissions", (CreateSubmissionRequest request) =>
{
    // Kiểm tra dữ liệu vào
    if (request.AssignmentId <= 0 || 
        string.IsNullOrWhiteSpace(request.StudentCode) || 
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new { message = "Du lieu nop bai khong hop le" });
    }

    // Tạo kết quả nộp bài
    var result = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode,
        "received",
        DateTimeOffset.UtcNow
    );

    // Lưu lượt nộp vào danh sách
    submissions.Add(result);

    // Trả về 201 Created
    return Results.Created($"/api/submissions/{result.Id}", result);
});

// 2. Thêm endpoint GET /api/submissions/{id:guid}
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    var submission = submissions.FirstOrDefault(s => s.Id == id);
    if (submission == null)
    {
        return Results.NotFound(new { message = "Khong tim thay luot nop bai" });
    }
    return Results.Ok(submission);
});

app.Run();

// Định nghĩa các Data Model (Records)
record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);