using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var repoUrl = args.Length > 2 ? args[2] : $"https://example.com/{studentCode}/signalr";

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource(); // "Cái chuông"

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    var time = DateTime.Now.ToString("HH:mm:ss");
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId.ToString()[..8]}... -> {m.Status} luc {time}");
    if (m.Status is "graded" or "failed")
    {
        if (m.Status == "failed")
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId.ToString()[..8]} cham LOI (failed)");
            Console.ForegroundColor = oldColor;
        }
        gradedNotification.TrySetResult();
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// ① REST: Lấy danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao de nop.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

// ② REST: Tạo lượt nộp
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, repoUrl));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var err = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi tao luot nop ({createResponse.StatusCode}): {err?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created!.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// ③ WatchSubmission: Vào nhóm theo ID lượt nộp
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

// ④ Chờ chuông kêu (hoặc timeout 10 giây)
var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// ⑤ GET đọc lại bằng REST để chốt trạng thái cuối cùng
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

// Tự động thoát sau khi nhận graded theo tiêu chí Slide 71

// Records
record AssignmentDto(int Id, string Title, string Description);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
