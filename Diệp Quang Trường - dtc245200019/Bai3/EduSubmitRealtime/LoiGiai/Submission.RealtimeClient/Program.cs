using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR (dùng chung 1 kết nối cho cả 2 lượt nộp)
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// Bài tập 3: Mỗi lượt nộp một TaskCompletionSource trong ConcurrentDictionary
var trackers = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    var shortId = m.SubmissionId.ToString()[..8];
    if (m.Status == "failed")
    {
        // Bài tập 2: nhận failed thì in cảnh báo màu đỏ
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {shortId} cham LOI (failed)");
        Console.ForegroundColor = prev;
    }
    else
    {
        Console.WriteLine($"Thong bao SignalR: {shortId} -> {m.Status}");
    }

    if ((m.Status is "graded" or "failed") &&
        trackers.TryGetValue(m.SubmissionId, out var tcs))
    {
        tcs.TrySetResult(m.Status);
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// Tạo lượt nộp 1 (hợp lệ https:// -> kết quả graded)
var res1 = await http.PostAsJsonAsync(
    "/api/assignments/1/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/SV001/signalr"));
if (res1.StatusCode != HttpStatusCode.Created) return;
var sub1 = (await res1.Content.ReadFromJsonAsync<SubmissionDto>())!;
trackers[sub1.Id] = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
Console.WriteLine($"Da tao SV001: {sub1.Id.ToString()[..8]} ({sub1.Status})");
await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());

// Tạo lượt nộp 2 (không có https:// -> kết quả failed)
var res2 = await http.PostAsJsonAsync(
    "/api/assignments/1/submissions",
    new CreateSubmissionRequest("SV002", "http://example.com/SV002/signalr"));
if (res2.StatusCode != HttpStatusCode.Created) return;
var sub2 = (await res2.Content.ReadFromJsonAsync<SubmissionDto>())!;
trackers[sub2.Id] = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
Console.WriteLine($"Da tao SV002: {sub2.Id.ToString()[..8]} ({sub2.Status})");
await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

// Chờ cả 2 lượt nộp hoàn tất (hoặc timeout 10 giây)
var allDone = Task.WhenAll(trackers[sub1.Id].Task, trackers[sub2.Id].Task);
await Task.WhenAny(allDone, Task.Delay(TimeSpan.FromSeconds(10)));

var sr1 = trackers[sub1.Id].Task.IsCompletedSuccessfully ? trackers[sub1.Id].Task.Result : "timeout";
var sr2 = trackers[sub2.Id].Task.IsCompletedSuccessfully ? trackers[sub2.Id].Task.Result : "timeout";

var rest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
var rest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

Console.WriteLine($"SV001: SignalR = {sr1}, REST = {rest1?.Status}");
Console.WriteLine($"SV002: SignalR = {sr2}, REST = {rest2?.Status}");

record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status,
    DateTimeOffset UpdatedAt);
