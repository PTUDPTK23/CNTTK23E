using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

// Kênh 1: HTTP Client cho REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: HubConnection cho SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource();

// Bước 1: Đăng ký lắng nghe thông điệp từ máy chủ TRƯỚC khi gọi StartAsync()
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (m.Status == "graded")
    {
        gradedNotification.TrySetResult();
    }
});

// Bước 2: Mở kết nối SignalR (negotiate + WebSocket)
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// Bước 3: Gọi REST lấy danh sách bài tập và tạo lượt nộp mới
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault(a => a.IsOpen);
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao dang mo.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} {selected.Title}");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));

// Quy tắc vàng từ Bài 2: Kiểm tra mã trạng thái trước khi đọc body
if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi tao luot nop ({(int)createResponse.StatusCode}): {error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created!.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// Bước 4: Đăng ký vào nhóm submission:{id} để nhận thông báo riêng cho lượt nộp này
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// Chờ thông báo graded tối đa 10 giây
var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// Bước 5: Chốt lại trạng thái cuối cùng từ nguồn sự thật REST
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

// Khai báo lại các record DTO (hai tiến trình không dùng chung assembly)
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
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