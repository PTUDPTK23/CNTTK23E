using System;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu mau luu trong bo nho, dung pham vi bai thuc hanh RESTful API.
var assignments = new List<AssignmentDto>
{
    new(1, "Bai tap REST API", "Thiet ke endpoint va xu ly yeu cau HTTP.", true),
    // Bai tap 2 duoc dong de kiem thu loi 400 ASSIGNMENT_CLOSED (ca 5h trong slide).
    new(2, "Bai tap Client", "Viet console client su dung HttpClient.", false)
};

var submissions = new List<SubmissionDto>();

app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "running"
}));

// Endpoint 1: Lay danh sach bai tap.
app.MapGet("/api/assignments", () => Results.Ok(assignments));

// Endpoint 2: Lay chi tiet mot bai tap.
app.MapGet("/api/assignments/{assignmentId:int}", IResult (int assignmentId) =>
{
    var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);

    return assignment is null
        ? Results.NotFound(new ApiError(
            "ASSIGNMENT_NOT_FOUND",
            "Khong tim thay bai tap."))
        : Results.Ok(assignment);
});

// Endpoint 3: Tao luot nop bai. Thu tu kiem tra: ton tai -> con mo -> du lieu hop le.
app.MapPost("/api/assignments/{assignmentId:int}/submissions",
    IResult (int assignmentId, CreateSubmissionRequest? request) =>
    {
        var assignment = assignments.FirstOrDefault(item => item.Id == assignmentId);

        if (assignment is null)
        {
            return Results.NotFound(new ApiError(
                "ASSIGNMENT_NOT_FOUND",
                "Khong tim thay bai tap."));
        }

        if (!assignment.IsOpen)
        {
            return Results.BadRequest(new ApiError(
                "ASSIGNMENT_CLOSED",
                "Bai tap da dong, khong the nop bai."));
        }

        if (request is null ||
            string.IsNullOrWhiteSpace(request.StudentCode) ||
            string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            return Results.BadRequest(new ApiError(
                "INVALID_SUBMISSION",
                "Thieu ma sinh vien hoac kho ma nguon."));
        }

        var submission = new SubmissionDto(
            Guid.NewGuid(),
            assignmentId,
            request.StudentCode.Trim(),
            request.RepositoryUrl.Trim(),
            "received",
            DateTimeOffset.UtcNow);

        submissions.Add(submission);

        // 201 Created kem Location de may khach doc lai tai nguyen vua tao.
        return Results.Created($"/api/submissions/{submission.Id}", submission);
    });

// Endpoint 4: Doc lai mot luot nop theo Guid.
app.MapGet("/api/submissions/{submissionId:guid}", IResult (Guid submissionId) =>
{
    var submission = submissions.FirstOrDefault(item => item.Id == submissionId);

    return submission is null
        ? Results.NotFound(new ApiError(
            "SUBMISSION_NOT_FOUND",
            "Khong tim thay luot nop."))
        : Results.Ok(submission);
});

app.Run();

// DTO phan hoi: cac truong do may chu tao/kiem soat.
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

// DTO dau vao: chi gom cac truong may khach duoc phep gui.
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);

// DTO phan hoi cua mot luot nop.
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

// Dinh dang loi thong nhat cho cac loi do handler tra ve.
record ApiError(string Code, string Message);
