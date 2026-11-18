using System.Net.Http.Json;

// 1. Lấy địa chỉ API từ tham số dòng lệnh; nếu không có thì dùng mặc định
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// 2. Gọi GET /api/assignments để lấy danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<Assignment>>("/api/assignments");
Console.WriteLine("Danh sach bai tap:");
foreach (var assignment in assignments ?? new List<Assignment>())
{
    Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
}

// 3. Gửi bài nộp: POST /api/submissions
var submissionRequest = new CreateSubmissionRequest(
    AssignmentId: 1,
    StudentCode: "DTC245200309",
    RepositoryUrl: "https://github.com/DTC245200309/EduSubmit"
);

var postResponse = await http.PostAsJsonAsync("/api/submissions", submissionRequest);
postResponse.EnsureSuccessStatusCode();

var result = await postResponse.Content.ReadFromJsonAsync<SubmissionResult>();
Console.WriteLine($"Da nop bai. Ma luot nop: {result?.Id}");
Console.WriteLine($"Trang thai: {result?.Status}");

// 4. Bước 12 - Mở rộng: Đọc lại lượt nộp vừa tạo qua GET /api/submissions/{id}
if (result != null)
{
    var getResponse = await http.GetAsync($"/api/submissions/{result.Id}");
    getResponse.EnsureSuccessStatusCode();

    var retrievedSubmission = await getResponse.Content.ReadFromJsonAsync<SubmissionResult>();
    Console.WriteLine();
    Console.WriteLine("Thong tin luot nop (doc lai tu API):");
    Console.WriteLine($"- Ma luot nop: {retrievedSubmission?.Id}");
    Console.WriteLine($"- Ma sinh vien: {retrievedSubmission?.StudentCode}");
    Console.WriteLine($"- Ma bai tap: {retrievedSubmission?.AssignmentId}");
    Console.WriteLine($"- Trang thai: {retrievedSubmission?.Status}");
    Console.WriteLine($"- Thoi gian nhan: {retrievedSubmission?.ReceivedAt}");
}

// Các kiểu dữ liệu trao đổi (định nghĩa độc lập với API, không dùng chung project reference)
record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl
);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt
);
