using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Bước 1: lấy danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var assignment in assignments ?? [])
{
    Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
}

// Bước 2: nộp bài cho bài tập đầu tiên và kiểm tra mã 201 Created
var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;
var createRequest = new CreateSubmissionRequest(
    StudentCode: "SV001",
    RepositoryUrl: "https://example.com/sv001/rest-api");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    createRequest);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var createdSubmission = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine();
Console.WriteLine($"Da tao luot nop: {createdSubmission?.Id}");
Console.WriteLine($"Trang thai ban dau: {createdSubmission?.Status}");

// Bước 3: đọc lại lượt nộp vừa tạo
var detail = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{createdSubmission?.Id}");
Console.WriteLine($"Doc lai luot nop: {detail?.StudentCode} - {detail?.RepositoryUrl}");

// Bước 4: cố ý gửi yêu cầu sai để quan sát phản hồi 400 và ApiError
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest(StudentCode: "", RepositoryUrl: ""));

Console.WriteLine();
Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
    {
        return;
    }

    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Bản sao DTO phía máy khách (chỉ dùng chung hợp đồng JSON, không tham chiếu project API)
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
