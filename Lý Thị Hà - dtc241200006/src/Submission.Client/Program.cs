using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Lấy danh sách bài tập trước để chọn ID hợp lệ
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
foreach (var a in assignments ?? []) {
    Console.WriteLine($"{a.Id}: {a.Title}");
}

// Bước 2: nộp bài, kiểm tra 201, lỗi thì đọc ApiError bằng một hàm chung
var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/..."));

if (createResponse.StatusCode != HttpStatusCode.Created) {
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content
    .ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");
    Console.WriteLine($"Doc lai luot nop: {created?.StudentCode} - {created?.RepositoryUrl}");
// Console.WriteLine($"Da nop thanh cong bai co ID: {created?.Id}");

static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// --- KHAI BÁO CÁC RECORD DTO (BẮT BUỘC NẰM Ở CUỐI CÙNG CỦA TỆP) ---
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);