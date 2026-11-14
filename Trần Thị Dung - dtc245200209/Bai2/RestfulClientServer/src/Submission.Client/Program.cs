using System.Net;
using System.Net.Http.Json;

// Nhận URL server từ tham số dòng lệnh hoặc mặc định localhost:5000
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// ==========================================
// NẤC 1: Lấy danh sách bài tập đang mở
// ==========================================
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var a in assignments?.Where(x => x.IsOpen) ?? [])
{
    Console.WriteLine($" - {a.Id}: {a.Title}");
}

// ==========================================
// NẤC 2: Nộp bài cho bài tập 1
// ==========================================
var selectedAssignmentId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");

// ==========================================
// NẤC 3: Đọc lại lượt nộp theo URL trong header Location
// ==========================================
if (createResponse.Headers.Location != null)
{
    var readBack = await http.GetFromJsonAsync<SubmissionDto>(createResponse.Headers.Location);
    Console.WriteLine($"Doc lai luot nop: {readBack?.StudentCode} - {readBack?.RepositoryUrl}");
}

// ==========================================
// NẤC 4: Thử gửi yêu cầu sai để quan sát lỗi 400
// ==========================================
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));

Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

// Hàm xử lý và in thông tin lỗi thống nhất
static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Khai báo lại DTO ở phía Client (chỉ chia sẻ hợp đồng JSON)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
