using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Lấy danh sách bài tập
var assignments =
    await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");

Console.WriteLine("Danh sach bai tap:");

foreach (var a in assignments ?? [])
{
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

// Chọn bài đầu tiên để nộp
var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;

// Gửi yêu cầu nộp bài
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest(
        "SV001",
        "https://example.com/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var created =
    await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();

Console.WriteLine($"Da tao luot nop: {created!.Id}");
Console.WriteLine($"Trang thai ban dau: {created.Status}");

// Đọc lại lượt nộp
var readResponse =
    await http.GetAsync($"/api/submissions/{created.Id}");

if (!readResponse.IsSuccessStatusCode)
{
    await PrintApiErrorAsync(readResponse);
    return;
}

var submission =
    await readResponse.Content.ReadFromJsonAsync<SubmissionDto>();

Console.WriteLine(
    $"Doc lai luot nop: {submission!.StudentCode} - {submission.RepositoryUrl}");

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error =
        await response.Content.ReadFromJsonAsync<ApiError>();

    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record ApiError(
    string Code,
    string Message);