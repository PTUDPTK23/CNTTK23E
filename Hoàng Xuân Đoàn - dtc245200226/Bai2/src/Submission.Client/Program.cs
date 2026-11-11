using System.Net;
using System.Net.Http.Json;

// Lấy URL từ tham số dòng lệnh hoặc mặc định cổng 5000[cite: 56, 85]
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5029";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

Console.WriteLine($"=== KET NOI API TAI: {apiBaseUrl} ===");

// Nấc 1: Lấy danh sách bài tập[cite: 85]
try
{
    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    Console.WriteLine("\n[Nấc 1] Danh sách bài tập đang mở:");
    foreach (var a in assignments ?? [])
    {
        Console.WriteLine($" - [{a.Id}] {a.Title} (IsOpen: {a.IsOpen})");
    }

    // Nấc 2: Nộp bài cho bài tập đầu tiên[cite: 87]
    var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;
    var createRequest = new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api");
    var createResponse = await http.PostAsJsonAsync($"/api/assignments/{selectedAssignmentId}/submissions", createRequest);

    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"\n[Nấc 2] Đã tạo lượt nộp thành công! ID: {created?.Id}");
        Console.WriteLine($"        Đọc lại tại Location: {createResponse.Headers.Location}");

        // Nấc 3: Đọc lại lượt nộp chi tiết theo đường dẫn Location[cite: 84]
        if (createResponse.Headers.Location != null)
        {
            var detail = await http.GetFromJsonAsync<SubmissionDto>(createResponse.Headers.Location);
            Console.WriteLine($"\n[Nấc 3] Đọc lại chi tiết - Sinh viên: {detail?.StudentCode}, Trạng thái: {detail?.Status}");
        }
    }
    else
    {
        await PrintApiErrorAsync(createResponse);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Lỗi kết nối] Không thể kết nối tới API: {ex.Message}[cite: 56, 88]");
}

// Nấc 4: Cố ý gửi dữ liệu sai để nhận lỗi 400 Bad Request[cite: 87]
try
{
    var invalidResp = await http.PostAsJsonAsync("/api/assignments/1/submissions", new CreateSubmissionRequest("", ""));
    Console.WriteLine($"\n[Nấc 4] Thử gửi dữ liệu sai: Mã HTTP {(int)invalidResp.StatusCode} {invalidResp.StatusCode}");
    await PrintApiErrorAsync(invalidResp);
}
catch (Exception ex)
{
    Console.WriteLine($"[Lỗi] {ex.Message}");
}

// Hàm hỗ trợ in lỗi chung dạng ApiError[cite: 87]
static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Lỗi từ API -> Code: {error?.Code} | Message: {error?.Message}");
}

// Khai báo DTO phía Client[cite: 70, 85]
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
