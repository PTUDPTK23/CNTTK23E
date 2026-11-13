
using System.Collections.Concurrent;

// Bai 1 - EduSubmit
// Submission.Api - Chuong trinh may chu
// Sinh vien: Dang Quoc Trung
// MSSV: DTC245200016

// 1. Khoi tao ung dung Web API
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 2. Du lieu mau: danh sach bai tap
var assignments = new[]
{
    new Assignment(1, "Bai tap 1", "Tao API dau tien"),
    new Assignment(2, "Bai tap 2", "Goi API tu Client")
};

// 3. Luu cac luot nop bai trong bo nho
var submissions =
    new ConcurrentDictionary<Guid, SubmissionResult>();

// 4. GET / - Kiem tra trang thai API
app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        service = "Submission.Api",
        status = "running"
    });
});

// 5. GET /api/assignments - Lay danh sach bai tap
app.MapGet("/api/assignments", () =>
{
    return Results.Ok(assignments);
});

// 6. POST /api/submissions - Tiep nhan bai nop
app.MapPost("/api/submissions",
    (CreateSubmissionRequest request) =>
    {
        // Kiem tra du lieu dau vao
        if (request.AssignmentId <= 0 ||
            string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new
            {
                message = "Du lieu nop bai khong hop le"
            });
        }

        // Tao ma dinh danh cho luot nop
        var submission = new SubmissionResult(
            Guid.NewGuid(),
            request.AssignmentId,
            request.StudentCode,
            "received",
            DateTimeOffset.UtcNow
        );

        // Luu luot nop vao bo nho
        submissions[submission.Id] = submission;

        // Tra ve HTTP 201 Created
        return Results.Created(
            $"/api/submissions/{submission.Id}",
            submission
        );
    }
);

// 7. GET /api/submissions/{id} - Tra cuu luot nop
app.MapGet("/api/submissions/{id:guid}", (Guid id) =>
{
    // Tim luot nop theo GUID
    if (submissions.TryGetValue(id, out var submission))
    {
        // Tim thay - HTTP 200 OK
        return Results.Ok(submission);
    }

    // Khong tim thay - HTTP 404 Not Found
    return Results.NotFound();
});

// 8. Chay Web API
app.Run();

// 9. Khai bao cac kieu du lieu
record Assignment(
    int Id,
    string Title,
    string Description
);

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
