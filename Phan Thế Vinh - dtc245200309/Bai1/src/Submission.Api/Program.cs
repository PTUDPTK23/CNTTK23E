var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu bài tập mẫu trong bộ nhớ
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Goi API tu client")
};

// Danh sách lưu trữ các lượt nộp bài trong bộ nhớ (Bước 12 - Mở rộng)
var submissions = new List<SubmissionResult>();

// GET / : kiểm tra trạng thái dịch vụ
app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// GET /api/assignments : lấy danh sách bài tập
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// POST /api/submissions : tiếp nhận bài nộp
app.MapPost("/api/submissions", (CreateSubmissionRequest request) =>
{
    // Kiểm tra tính hợp lệ của dữ liệu đầu vào
    if (request.AssignmentId <= 0 ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new
        {
            message = "Du lieu nop bai khong hop le"
        });
    }

    // Tạo bản ghi lượt nộp bài mới với trạng thái "received"
    var submission = new SubmissionResult(
        Guid.NewGuid(),
        request.AssignmentId,
        request.StudentCode,
        "received",
        DateTimeOffset.UtcNow
    );

    // Lưu trữ trong bộ nhớ
    lock (submissions)
    {
        submissions.Add(submission);
    }

    // Trả về HTTP 201 Created kèm header Location
    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission
    );
});

// GET /api/submissions/{id:guid} : tra cứu chi tiết lượt nộp theo GUID (Bước 12 - Mở rộng)
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    SubmissionResult? submission;
    lock (submissions)
    {
        submission = submissions.FirstOrDefault(s => s.Id == id);
    }

    if (submission is null)
    {
        return Results.NotFound(new
        {
            message = $"Khong tim thay luot nop voi ma: {id}"
        });
    }

    return Results.Ok(submission);
});

app.Run();

// Các hợp đồng dữ liệu trao đổi qua API
record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl
);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt
);
