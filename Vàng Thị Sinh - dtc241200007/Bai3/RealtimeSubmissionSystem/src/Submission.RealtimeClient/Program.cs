using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

Console.WriteLine($"=== CLIENT ĐANG KẾT NỐI TỚI: {apiBaseUrl} ===");

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// 1. Lấy danh sách bài tập từ REST API
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Không tìm thấy bài tập nào trên hệ thống!");
    return;
}
Console.WriteLine($"Đã chọn bài tập ID: {selected.Id} - {selected.Title}");

// Kênh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m => {
    Console.WriteLine($"[SignalR Realtime] Bài nộp {m.Id} -> Trạng thái: {m.Status.ToUpper()}");
    if (m.Status == "graded")
    {
        gradedNotification.TrySetResult();
    }
});

await connection.StartAsync();
Console.WriteLine("Đã kết nối thành công tới SignalR Hub.");

// 2. Tạo lượt nộp bài qua POST REST API
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://github.com/{studentCode}/realtime-signalr")
);

// Kiểm tra mã 201 Created trước khi đọc body
if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Lỗi tạo lượt nộp [{err?.Code}]: {err?.Message}");
    Console.ResetColor();
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
if (created is null) return;

Console.WriteLine($"Trạng thái REST ban đầu: {created.Status}");

// 3. Gọi WatchSubmission để đăng ký vào Group của bài nộp trên Hub
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine($"Đang theo dõi SignalR Group cho submission: {created.Id}");

// 4. Dùng Task.WhenAny: chờ chuông graded HOẶC timeout 10 giây
var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10))
);

if (completed != gradedNotification.Task)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Quá thời gian chờ thông báo graded qua SignalR.");
    Console.ResetColor();
}

// 5. Chốt lại bằng REST: Đọc lại trạng thái cuối cùng từ REST API
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n==> TRẠNG THÁI CUỐI LẤY TỪ REST: {latest?.Status?.ToUpper()} (Cập nhật lúc: {latest?.UpdatedAt:HH:mm:ss})");
Console.ResetColor();

// --- Các Record DTO dùng chung ---
record AssignmentDto(int Id, string Title, string Description);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid Id, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);