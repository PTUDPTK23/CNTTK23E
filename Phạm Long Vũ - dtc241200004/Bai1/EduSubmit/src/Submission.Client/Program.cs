using System.Net.Http.Json;

// Lấy địa chỉ API từ tham số dòng lệnh, mặc định http://localhost:5000
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine($"=== KET NOI TOI API TAI: {apiBaseUrl} ===");

// Goi GET /api/assignments
Console.WriteLine("\n[1] Lay danh sach bai tap:");
var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
foreach (var item in assignments ?? new List<Assignment>())
{
    Console.WriteLine($"  - {item.Id}: {item.Title} ({item.Description})");
}

// Goi POST /api/submissions
Console.WriteLine("\n[2] Gui bai nop:");
var request = new CreateSubmissionRequest(
    AssignmentId: 1,
    StudentCode: "DTC241200004",
    RepositoryUrl: "https://github.com/phamlongvuTN/EduSubmit-DTC241200004");

var response = await http.PostAsJsonAsync("/api/submissions", request);
response.EnsureSuccessStatusCode();

var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
Console.WriteLine($"  - Da nop bai thanh cong! Ma luot nop: {result?.Id}");
Console.WriteLine($"  - Trang thai: {result?.Status}");
Console.WriteLine($"  - Thoi gian: {result?.ReceivedAt}");

// Goi GET /api/submissions/{id} - Doc lai luot nop vua tao (Buoc 12 mo rong)
if (result is not null)
{
    Console.WriteLine($"\n[3] Kiem tra lai luot nop (GET /api/submissions/{result.Id}):");
    var checkResponse = await http.GetAsync($"/api/submissions/{result.Id}");
    checkResponse.EnsureSuccessStatusCode();

    var detail = await checkResponse.Content.ReadFromJsonAsync<SubmissionResult>();
    Console.WriteLine($"  - Xac nhan tu Server: MSSV {detail?.StudentCode} - Bai {detail?.AssignmentId} - Trang thai: {detail?.Status}");
}

// Khai bao lai hop dong du lieu (Client doc lap, khong tham chieu truc tiep code API)
record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt);
