using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource(); // "cái chuông" chờ trạng thái cuối

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status}");
    if (m.Status == "graded" || m.Status == "failed")
    {
        if (m.Status == "failed")
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId} cham LOI (failed)");
            Console.ForegroundColor = prevColor;
        }
        gradedNotification.TrySetResult();
    }
});

// Mở kết nối SignalR
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// Bước 1: Lấy danh sách bài tập qua REST
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong tim thay bai tap nao.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

// Bước 2: Tạo lượt nộp bài qua REST
var repoUrl = $"https://example.com/{studentCode}/signalr";
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, repoUrl));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi khi tao luot nop: {error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created!.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created!.Status}");
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// Bước 3: Đăng ký theo dõi lượt nộp qua SignalR (vào nhóm submission:{id})
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

// Bước 4: Chờ thông báo graded/failed từ máy chủ (tối đa 10 giây)
var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// Bước 5: Đọc lại trạng thái cuối cùng bằng REST (Nguồn sự thật)
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

// --- DTO & Records ---
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
