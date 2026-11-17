var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint va DTO.", true),
    new(2, "Bai tap Client", "Viet console client HttpClient.", true),
    new(3, "Bai tap da dong", "Khong nhan bai nop nua.", false)
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

app.MapPost("/api/assignments/{assignmentId:int}/submissions", (int assignmentId, CreateSubmissionRequest? request) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
    if (assignment is null)
        return Results.NotFound(new ApiError("ASSIGNMENT_NOT_FOUND", "Khong tim thay bai tap."));

    if (!assignment.IsOpen)
        return Results.BadRequest(new ApiError("ASSIGNMENT_CLOSED", "Bai tap da dong."));

    if (request is null || string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "Can co ma sinh vien va URL repository."));

    if (!Uri.TryCreate(request.RepositoryUrl.Trim(), UriKind.Absolute, out var repositoryUri) ||
        (repositoryUri.Scheme != Uri.UriSchemeHttp && repositoryUri.Scheme != Uri.UriSchemeHttps))
        return Results.BadRequest(new ApiError("INVALID_SUBMISSION", "RepositoryUrl phai la URL HTTP/HTTPS hop le."));

    var submission = new SubmissionDto(
        Guid.NewGuid(), assignmentId, request.StudentCode.Trim(), repositoryUri.ToString(),
        "received", DateTimeOffset.UtcNow);
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

app.Run();

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
