using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Nấc 1: lấy danh sách bài tập đang mở
Console.WriteLine("Danh sach bai tap dang mo:");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
foreach (var a in assignments ?? [])
{
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

// Nấc 2: nộp bài
var selectedAssignmentId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;
var createRequest = new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api");
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    createRequest);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");
Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");

// Nấc 3: đọc lại chi tiết lượt nộp vừa tạo
if (created != null)
{
    var detail = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Doc lai luot nop: {detail?.Id} ({detail?.StudentCode} - {detail?.RepositoryUrl})");
}

// Nấc 4: cố ý gửi dữ liệu sai để quan sát lỗi 400
var invalidRequest = new CreateSubmissionRequest("", "");
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    invalidRequest);

Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Khai báo lại DTO ở client (chỉ chia sẻ hợp đồng JSON)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
