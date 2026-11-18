using System.Net;
using System.Net.Http.Json;

// Lấy địa chỉ API từ tham số dòng lệnh args[0], nếu không truyền thì dùng mặc định http://localhost:5000
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine("--- DANH SÁCH BÀI TẬP DẠNG MỞ ---");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
foreach (var a in assignments ?? [])
{
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

Console.WriteLine("\n--- THỬ NỘP BÀI THÀNH CÔNG (201 CREATED) ---");
var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api")
);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
}
else
{
    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");

    // Đọc lại lượt nộp
    if (created is not null)
    {
        var getSubResponse = await http.GetAsync($"/api/submissions/{created.Id}");
        if (getSubResponse.IsSuccessStatusCode)
        {
            var subDetail = await getSubResponse.Content.ReadFromJsonAsync<SubmissionDto>();
            Console.WriteLine($"Doc lai luot nop: {subDetail?.StudentCode} - {subDetail?.RepositoryUrl}");
        }
    }
}

Console.WriteLine("\n--- THỬ GỬI YÊU CẦU LỖI (400 BAD REQUEST) ---");
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", "")
);

if (!invalidResponse.IsSuccessStatusCode)
{
    await PrintApiErrorAsync(invalidResponse);
}

// Hàm dùng chung để in body lỗi ApiError
static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Thu gui yeu cau loi: {(int)r.StatusCode} {r.StatusCode}");
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// DTO hợp đồng với Server
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);