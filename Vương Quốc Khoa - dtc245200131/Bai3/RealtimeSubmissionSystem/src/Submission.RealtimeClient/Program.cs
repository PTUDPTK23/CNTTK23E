// ==============================================================================
// EDUSUBMIT REALTIME - BÀI 3: GIAO TIẾP THỜI GIAN THỰC VỚI SIGNALR
// Submission.RealtimeClient - Ứng dụng Console Máy Khách (REST + SignalR)
// ==============================================================================

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

// ── KÊNH 1: REST API (YÊU CẦU - PHẢN HỒI) ───────────────────────────────────
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// ── KÊNH 2: SIGNALR (KẾT NỐI HAI CHIỀU THỜI GIAN THỰC) ───────────────────────
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource(); // Chuông chờ sự kiện "graded"

// Đăng ký nhận thông báo thời gian thực từ Server
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt:HH:mm:ss}");
    if (m.Status == "graded")
    {
        gradedNotification.TrySetResult();
    }
});

// ① Mở kết nối SignalR trước
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// ② Gọi REST API lấy danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao trong he thong.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

// ③ Gọi REST API POST để tạo lượt nộp bài
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi tao luot nop: {err?.Code} - {err?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created!.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// ④ Gọi phương thức Hub WatchSubmission để gia nhập nhóm submission:{id}
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

// ⑤ Chờ sự kiện "graded" được máy chủ đẩy về hoặc hết thời gian 10 giây
var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// ⑥ Quy tắc vàng: Đọc lại trạng thái chốt hạ bằng REST API (Nguồn sự thật)
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

// ── KHAI BÁO DTO RIÊNG CỦA MÁY KHÁCH (CLIENT DTOs) ─────────────────────────
public record AssignmentDto(int Id, string Title, string Description, DateTimeOffset Deadline, bool IsOpen);

public record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

public record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt
);

public record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt
);

public record ApiError(string Code, string Message);
