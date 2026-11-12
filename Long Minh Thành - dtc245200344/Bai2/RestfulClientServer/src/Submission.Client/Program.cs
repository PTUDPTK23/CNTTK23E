using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

// Nấc 1: Lấy danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var a in assignments ?? []) {
    Console.WriteLine($" - {a.Id}: {a.Title}");
}

// Nấc 2: Nộp bài cho bài tập đầu tiên
var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created) {
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");

// Nấc 3: Đọc lại lượt nộp theo header Location
if (createResponse.Headers.Location is not null) {
    var readBack = await http.GetFromJsonAsync<SubmissionDto>(createResponse.Headers.Location);
    Console.WriteLine($"Doc lai luot nop: {readBack?.StudentCode} - {readBack?.RepositoryUrl}");
}

// Nấc 4: Thử gửi yêu cầu lỗi (dữ liệu rỗng) để quan sát mã 400 và ApiError
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));

Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

// Hàm dùng chung đọc lỗi ApiError
static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// DTO records của Client (khớp hợp đồng JSON)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
