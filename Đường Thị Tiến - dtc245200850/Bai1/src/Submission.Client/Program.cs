using System.Net.Http.Json;

// 1. Lấy địa chỉ API từ tham số truyền vào hoặc dùng mặc định
var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

// 2. Khởi tạo HttpClient
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

// 3. Gọi GET lấy danh sách bài tập
var assignments = await client.GetFromJsonAsync<List<Assignment>>("/api/assignments");
Console.WriteLine("Danh sach bai tap:");
if (assignments != null)
{
    foreach (var item in assignments)
    {
        Console.WriteLine($"- {item.Id}: {item.Title}");
    }
}

// 4. Gọi POST nộp bài
var request = new CreateSubmissionRequest(1, "SV001", "https://github.com/demo/repo");
var response = await client.PostAsJsonAsync("/api/submissions", request);
response.EnsureSuccessStatusCode();

var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
if (result != null)
{
    Console.WriteLine($"Da nop bai. Ma luot nop: {result.Id}  Trang thai: {result.Status}");

    // 5. Gọi GET /api/submissions/{id} để đọc lại lượt nộp vừa tạo
    var fetchedSubmission = await client.GetFromJsonAsync<SubmissionResult>($"/api/submissions/{result.Id}");
    if (fetchedSubmission != null)
    {
        Console.WriteLine($"[Doc lai thanh cong] Ma: {fetchedSubmission.Id} | SV: {fetchedSubmission.StudentCode} | Thoi gian: {fetchedSubmission.ReceivedAt}");
    }
}

// Định nghĩa các Record cho Client
record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);