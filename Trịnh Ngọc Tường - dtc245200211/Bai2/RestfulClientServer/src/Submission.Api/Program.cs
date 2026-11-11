var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu mau trong bo nho (Step 3)
var assignments = new List<AssignmentDto> {
    new(1, "Bai tap REST API", "Thiet ke endpoint va hop dong API", true),
    new(2, "Bai tap Client", "Viet console client voi HttpClient", true),
    new(3, "Bai tap da dong", "Bai tap da het han nop", false)
};
var submissions = new List<SubmissionDto>();

// Step 2: GET / : kiem tra may chu dang chay
app.MapGet("/", () => Results.Ok(new {
    service = "Submission.Api",
    status = "running"
}));

// Step 3: GET /api/assignments : danh sach bai tap
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Step 4: GET /api/assignments/{assignmentId:int} : chi tiet mot bai tap
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) => {
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Step 5 & Step 9: POST /api/assignments/{assignmentId:int}/submissions : nop bai
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) => {
        var assignment = assignments.FirstOrDefault(i => i.Id == assignmentId);
        if (assignment is null)
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        if (!assignment.IsOpen)
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop."));
        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));

        var duplicated = submissions.Any(item =>
            item.AssignmentId == assignmentId &&
            item.StudentCode.Equals(request.StudentCode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicated)
            return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));

        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            request.StudentCode.Trim(),
            request.RepositoryUrl.Trim(),
            "received",
            DateTimeOffset.UtcNow);
        submissions.Add(submission);
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// Step 6: GET /api/submissions/{submissionId:guid} : doc lai luot nop
app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

// Step 9 (mo rong): DELETE /api/submissions/{submissionId:guid} : rut luot nop
app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) => {
    var s = submissions.FirstOrDefault(i => i.Id == submissionId);
    if (s is null)
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));
    submissions.Remove(s);
    return Results.NoContent();
});

app.Run();

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
