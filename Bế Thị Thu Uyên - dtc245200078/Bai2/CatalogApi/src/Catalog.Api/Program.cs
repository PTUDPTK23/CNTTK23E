
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu bai tap mau
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap RESTful API",
        "Xay dung API quan ly san pham", true),

    new(2, "Bai tap EduSubmit",
        "Xay dung API quan ly bai tap va nop bai", true),

    new(3, "Bai tap nang cao",
        "Thuc hanh kiem tra va xu ly loi API", false)
};

// Du lieu bai nop trong bo nho
var submissions = new List<SubmissionDto>();

// GET /
app.MapGet("/", () =>
{
    return Results.Json(new
    {
        service = "EduSubmit.Api",
        status = "running"
    });
});

// GET /api/assignments
app.MapGet("/api/assignments", () =>
{
    return Results.Ok(assignments);
});

// GET /api/assignments/{assignmentId:int}
app.MapGet("/api/assignments/{assignmentId:int}", (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId
    );

    if (assignment == null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."
        ));
    }

    return Results.Ok(assignment);
});

// POST /api/assignments/{assignmentId:int}/submissions
app.MapPost(
    "/api/assignments/{assignmentId:int}/submissions",
    (int assignmentId, CreateSubmissionRequest request) =>
{
    var assignment = assignments.FirstOrDefault(
        a => a.Id == assignmentId
    );

    if (assignment == null)
    {
        return Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."
        ));
    }

    if (!assignment.IsOpen)
    {
        return Results.BadRequest(new ApiError(
            "ASSIGNMENT_CLOSED",
            "Bai tap da dong, khong the nop bai."
        ));
    }

    if (string.IsNullOrWhiteSpace(request.StudentCode) ||
        string.IsNullOrWhiteSpace(request.RepositoryUrl) ||
        !Uri.TryCreate(
            request.RepositoryUrl,
            UriKind.Absolute,
            out var uri
        ) ||
        (uri.Scheme != Uri.UriSchemeHttp &&
         uri.Scheme != Uri.UriSchemeHttps))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_SUBMISSION",
            "Ma sinh vien hoac RepositoryUrl khong hop le."
        ));
    }

    // Tao bai nop moi
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
        submission
    );
});

// GET /api/submissions/{submissionId:guid}
app.MapGet(
    "/api/submissions/{submissionId:guid}",
    (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(
        s => s.Id == submissionId
    );

    if (submission == null)
    {
        return Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay bai nop."
        ));
    }

    return Results.Ok(submission);
});

app.Run();

// DTO
record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen
);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl
);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt
);

record ApiError(
    string Code,
    string Message
);
