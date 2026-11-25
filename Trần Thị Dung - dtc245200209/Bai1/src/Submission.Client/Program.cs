using System.Net.Http.Json;

// 1. Địa chỉ API từ tham số dòng lệnh hoặc mặc định localhost:5000
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

// 2. Khởi tạo HttpClient với BaseAddress
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine($"Ket noi toi API tai: {apiBaseUrl}\n");

// 3. Yêu cầu 1: GET /api/assignments
Console.WriteLine("--- 1. Lay danh sach bai tap ---");
var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
foreach (var assignment in assignments ?? new List<Assignment>())
{
    Console.WriteLine($"- [{assignment.Id}] {assignment.Title}: {assignment.Description}");
}

// 4. Yêu cầu 2: POST /api/submissions
Console.WriteLine("\n--- 2. Gui bai nop moi ---");
var request = new CreateSubmissionRequest(
    AssignmentId: 1,
    StudentCode: "DTC245200209",
    RepositoryUrl: "https://github.com/DTC245200209/EduSubmit-DTC245200209"
);

var response = await http.PostAsJsonAsync("/api/submissions", request);
response.EnsureSuccessStatusCode();

var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
Console.WriteLine($"Da nop bai thanh cong!");
Console.WriteLine($"Ma luot nop: {result?.Id}");
Console.WriteLine($"Trang thai: {result?.Status}");
Console.WriteLine($"Thoi gian nhan: {result?.ReceivedAt}");

// 5. Bước 12 (Bài mở rộng): Tra cứu lại lượt nộp vừa tạo
if (result != null)
{
    Console.WriteLine("\n--- 3. Tra cuu lai luot nop vua tao (Buoc 12 - Bai mo rong) ---");
    var getResponse = await http.GetAsync($"/api/submissions/{result.Id}");
    if (getResponse.IsSuccessStatusCode)
    {
        var fetchedSubmission = await getResponse.Content.ReadFromJsonAsync<SubmissionResult>();
        Console.WriteLine($"Tra cuu thanh cong luot nop {fetchedSubmission?.Id}:");
        Console.WriteLine($" - Sinh vien: {fetchedSubmission?.StudentCode}");
        Console.WriteLine($" - Bai tap ID: {fetchedSubmission?.AssignmentId}");
        Console.WriteLine($" - Trang thai: {fetchedSubmission?.Status}");
        Console.WriteLine($" - Thoi gian: {fetchedSubmission?.ReceivedAt}");
    }
    else
    {
        Console.WriteLine($"Khong the tra cuu luot nop: {getResponse.StatusCode}");
    }
}

// Hợp đồng dữ liệu tự định nghĩa phía Client (không tham chiếu API)
record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);
