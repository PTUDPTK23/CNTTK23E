var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint RESTful API", true),
    new(2, "Bai tap Client", "Viet console client su dung HttpClient", true)
};

var submissions = new List<SubmissionDto>();

app.MapGet("/", () => Results.Ok(new { service = "Submission.Api", status = "running" }));

app.MapGet("/api/assignments", () => Results.Ok(assignments));

app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    return assignment is null
        ? Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest? request) =>
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
        {
            return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));
        }

        if (!assignment.IsOpen)
        {
            return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong, khong the nop bai."));
        }

        if (request is null || string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Thieu ma sinh vien hoac kho ma nguon."));
        }

        var studentCode = request.StudentCode.Trim();
        if (submissions.Any(item => item.AssignmentId == assignmentId &&
                                    string.Equals(item.StudentCode, studentCode, StringComparison.OrdinalIgnoreCase)))
        {
            return Results.Conflict(new ApiError("DUPLICATE_SUBMISSION", "Sinh vien da nop bai tap nay."));
        }

        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            studentCode,
            request.RepositoryUrl.Trim(),
            "received",
            DateTimeOffset.UtcNow);

        submissions.Add(submission);
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

app.MapGet("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);
    return submission is null
        ? Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.MapDelete("/api/submissions/{submissionId:guid}", (Guid submissionId) =>
{
    var index = submissions.FindIndex(item => item.Id == submissionId);
    if (index < 0)
    {
        return Results.NotFound(new ApiError("SUBMISSION_NOT_FOUND", "Khong tim thay luot nop."));
    }

    submissions.RemoveAt(index);
    return Results.NoContent();
});

app.Run();

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record ApiError(string Code, string Message);
