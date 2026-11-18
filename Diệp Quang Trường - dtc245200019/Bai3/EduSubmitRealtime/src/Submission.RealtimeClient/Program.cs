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

var gradedNotification = new TaskCompletionSource(); // "cái chuông"

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (m.Status == "graded") gradedNotification.TrySetResult();
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();       // ① REST: danh sách
if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao.");
    return;
}
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

var createResponse = await http.PostAsJsonAsync(    // ② REST: tạo lượt nộp
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));
if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"Loi {(int)createResponse.StatusCode}: {error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created!.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

await connection.InvokeAsync("WatchSubmission", created.Id.ToString()); // ③
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

var completed = await Task.WhenAny(gradedNotification.Task,             // ④
                                   Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != gradedNotification.Task)
    Console.WriteLine("Qua thoi gian cho thong bao graded.");

var latest = await http.GetFromJsonAsync<SubmissionDto>(                // ⑤
    $"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

record AssignmentDto(int Id, string Title, string Status);

record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(Guid SubmissionId, string Status,
    DateTimeOffset UpdatedAt);

record ApiError(string Code, string Message);
