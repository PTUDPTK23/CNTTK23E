using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var mode = args.Length > 2 ? args[2].ToLowerInvariant() : "normal";

// Kênh 1: REST Client
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Kênh 2: SignalR Hub Connection
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// Quản lý các TaskCompletionSource cho từng lượt nộp (Hỗ trợ theo dõi nhiều lượt nộp cùng lúc)
var pendingTasks = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    var timeStr = m.UpdatedAt.ToLocalTime().ToString("HH:mm:ss");
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {timeStr}");

    if (m.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
    {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId} cham LOI (failed)");
        Console.ForegroundColor = prevColor;
    }

    if (pendingTasks.TryGetValue(m.SubmissionId, out var tcs))
    {
        if (m.Status is "graded" or "failed")
        {
            tcs.TrySetResult(m.Status);
        }
    }
});

// Mở kết nối SignalR
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// Lấy danh sách bài tập qua REST
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault(a => a.IsOpen);
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao dang mo.");
    return;
}

if (mode == "--two")
{
    // =========================================================================
    // VẬN DỤNG: THEO DÕI 2 LƯỢT NỘP CÙNG LÚC TRÊN MỘT KẾT NỐI
    // =========================================================================
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    // Tạo lượt nộp 1: SV001 (hợp lệ -> graded)
    var req1 = new CreateSubmissionRequest("SV001", "https://example.com/sv001/signalr");
    var res1 = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions", req1);
    var sub1 = await res1.Content.ReadFromJsonAsync<SubmissionDto>();
    var tcs1 = new TaskCompletionSource<string>();
    pendingTasks[sub1!.Id] = tcs1;
    Console.WriteLine($"Da tao SV001: {sub1.Id} (received)");

    // Tạo lượt nộp 2: SV002 (link http:// -> failed theo Bài tập 2 Vận dụng)
    var req2 = new CreateSubmissionRequest("SV002", "http://example.com/sv002/failed-demo");
    var res2 = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions", req2);
    var sub2 = await res2.Content.ReadFromJsonAsync<SubmissionDto>();
    var tcs2 = new TaskCompletionSource<string>();
    pendingTasks[sub2!.Id] = tcs2;
    Console.WriteLine($"Da tao SV002: {sub2.Id} (received)");

    // Watch cả hai lượt nộp trên cùng một kết nối SignalR
    await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
    await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

    // Chờ cả 2 hoàn tất hoặc hết thời gian
    await Task.WhenAll(
        Task.WhenAny(tcs1.Task, Task.Delay(10000)),
        Task.WhenAny(tcs2.Task, Task.Delay(10000))
    );

    // Đọc lại trạng thái chốt từ REST
    var latest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
    var latest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

    Console.WriteLine($"SV001: SignalR = {tcs1.Task.Result}, REST = {latest1?.Status}");
    Console.WriteLine($"SV002: SignalR = {tcs2.Task.Result}, REST = {latest2?.Status}");
}
else
{
    // =========================================================================
    // CHẾ ĐỘ CHUẨN (8 DÒNG THEO ĐÚNG SLIDE 57 VÀ SLIDE 71)
    // =========================================================================
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    var repoUrl = mode == "--failed"
        ? $"http://example.com/{studentCode}/signalr" // http:// se ra failed
        : $"https://example.com/{studentCode}/signalr"; // https:// se ra graded

    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentCode, repoUrl));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Khong the nop bai: {(int)createResponse.StatusCode} {createResponse.StatusCode} ({err?.Code}: {err?.Message})");
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao luot nop: {created!.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    var tcs = new TaskCompletionSource<string>();
    pendingTasks[created.Id] = tcs;

    // Đăng ký theo dõi lượt nộp qua SignalR Hub
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

    // Chờ thông báo SignalR (hoàn tất khi nhận graded hoặc failed, timeout 10s)
    var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (completed != tcs.Task)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    }

    // Đọc lại từ REST để chốt trạng thái (nguồn sự thật)
    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
}

// Tự động kết thúc và giải phóng tài nguyên
await connection.StopAsync();

// =========================================================================
// KHAI BÁO DTO PHÍA CLIENT
// =========================================================================

public record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
public record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
public record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
public record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
public record ApiError(string Code, string Message);
