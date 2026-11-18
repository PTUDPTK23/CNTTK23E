var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke RESTful API va cac endpoint.", true),
    new(2, "Bai tap Client", "Viet ung dung HttpClient.", false)
};

var submissions = new List<SubmissionDto>();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// Endpoint 1: Danh sach bai tap
app.MapGet("/api/assignments", () =>
    Results.Ok(assignments));

// Endpoint 2: Chi tiet bai tap
app.MapGet("/api/assignments/{assignmentId:int}",
    (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(
        item => item.Id == assignmentId);

    if (assignment is null)
        return (IResult)Results.NotFound(
            new ApiError("ASSIGNMENT_NOT_FOUND",
                "Khong tim thay bai tap."));

    return Results.Ok(assignment);
});

// Endpoint 3: Sinh vien nop bai
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest? request) =>
{
    var assignment = assignments.FirstOrDefault(
        item => item.Id == assignmentId);

    if (assignment is null)
        return (IResult)Results.NotFound(
            new ApiError("ASSIGNMENT_NOT_FOUND",
                "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(
            new ApiError("ASSIGNMENT_CLOSED",
                "Bai tap da dong, khong the nop."));

    if (request is null ||
        string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(
            new ApiError("INVALID_SUBMISSION",
                "Thieu ma sinh vien hoac link ma nguon."));

    var submission = new SubmissionDto(
        Guid.NewGuid(),
        assignmentId,
        request.StudentCode.Trim(),
        request.RepositoryUrl.Trim(),
        "received",
        DateTimeOffset.UtcNow
    );

    submissions.Add(submission);

    return Results.Created(
        $"/api/submissions/{submission.Id}",
        submission);
});

// Endpoint 4: Doc lai luot nop
app.MapGet("/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(
        item => item.Id == submissionId);

    if (submission is null)
        return (IResult)Results.NotFound(
            new ApiError("SUBMISSION_NOT_FOUND",
                "Khong tim thay luot nop."));

    return Results.Ok(submission);
});

app.Run();

record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

record CreateSubmissionRequest(
    string? StudentCode,
    string? RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record ApiError(string Code, string Message);
