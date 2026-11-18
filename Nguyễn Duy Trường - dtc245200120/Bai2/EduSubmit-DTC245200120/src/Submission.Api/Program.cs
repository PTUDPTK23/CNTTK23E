
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ==========================================
// 1. DU LIEU MAU
// ==========================================

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap 1", "Tao API dau tien", true),
    new(2, "Bai tap 2", "Xay dung HTTP Client", true),
    new(3, "Bai tap 3", "Bai tap da dong", false)
};

// Luu danh sach bai nop trong RAM
var submissions = new List<SubmissionDto>();

// Dong bo khi nhieu request truy cap
var submissionsLock = new object();

// ==========================================
// 2. GET / - KIEM TRA MAY CHU
// ==========================================

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// ==========================================
// 3. GET DANH SACH BAI TAP
// ==========================================

app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// ==========================================
// 4. GET CHI TIET BAI TAP
// ==========================================

app.MapGet(
    "/api/assignments/{assignmentId:int}",
    (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    return Results.Ok(assignment);
});

// ==========================================
// 5. POST - NOP BAI TAP
// ==========================================

app.MapPost(
    "/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    // Kiem tra bai tap co ton tai
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId);

    if (assignment is null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."));
    }

    // Kiem tra bai tap con mo
    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED",
            "Bai tap da dong."));
    }

    // Kiem tra du lieu nop bai
    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Thieu ma sinh vien hoac kho ma nguon."));
    }

    var studentCode = request.StudentCode.Trim();
    var repositoryUrl = request.RepositoryUrl.Trim();

    // Kiem tra nop trung va luu trong cung mot lock
    lock (submissionsLock)
    {
        var duplicated = submissions.Any(s =>
            s.AssignmentId == assignmentId &&
            string.Equals(
                s.StudentCode,
                studentCode,
                StringComparison.OrdinalIgnoreCase));

        if (duplicated)
        {
            return Results.Conflict(new ApiError(
                "DUPLICATE_SUBMISSION",
                "Sinh vien da nop bai tap nay."));
        }

        // Tao luot nop moi
        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            studentCode,
            repositoryUrl,
            "received",
            DateTimeOffset.UtcNow
        );

        // Luu vao RAM
        submissions.Add(submission);

        // Tra ve 201 Created + Location
        return Results.Created(
            $"/api/submissions/{submission.Id}",
            submission);
    }
});

// ==========================================
// 6. GET - DOC LAI LUOT NOP
// ==========================================

app.MapGet(
    "/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    SubmissionDto? submission;

    lock (submissionsLock)
    {
        submission = submissions.FirstOrDefault(
            s => s.Id == submissionId);
    }

    if (submission is null)
    {
        return Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay luot nop."));
    }

    return Results.Ok(submission);
});

// ==========================================
// 7. DELETE - XOA LUOT NOP
// ==========================================

app.MapDelete(
    "/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    lock (submissionsLock)
    {
        var submission = submissions.FirstOrDefault(
            s => s.Id == submissionId);

        // Khong tim thay -> 404
        if (submission is null)
        {
            return Results.NotFound(new ApiError(
                "SUBMISSION_NOT_FOUND",
                "Khong tim thay luot nop."));
        }

        // Xoa luot nop
        submissions.Remove(submission);
    }

    // Xoa thanh cong -> 204
    return Results.NoContent();
});

// ==========================================
// 8. KHOI DONG API
// ==========================================

app.Run();

// ==========================================
// 9. DINH NGHIA CAC DTO
// ==========================================

// Thong tin bai tap
record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen
);

// Du lieu Client gui len
record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
);

// Ket qua luot nop bai
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt
);

// Dinh dang loi chung
record ApiError(
    string Code,
    string Message
);
