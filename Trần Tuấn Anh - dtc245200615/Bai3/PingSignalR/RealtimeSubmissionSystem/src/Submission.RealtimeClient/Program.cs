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

var gradedNotification = new TaskCompletionSource();

// Đăng ký nghe thông báo trạng thái từ máy chủ
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (m.Status == "graded")
    {
        gradedNotification.TrySetResult();
    }
});

// 1. Mở kết nối SignalR
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// 2. Lấy danh sách bài tập qua REST
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} {selected.Title}");

// 3. Tạo lượt nộp mới qua REST
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi tao luot nop ({(int)createResponse.StatusCode}): {err?.Code} - {err?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created!.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 4. Đăng ký theo dõi nhóm của riêng lượt nộp này qua SignalR
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// 5. Chờ nhận thông báo graded (tối đa 10 giây)
var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// 6. Đọc lại trạng thái cuối cùng bằng REST (nguồn sự thật)
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

// --- Các record DTO khớp với máy chủ ---
record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);