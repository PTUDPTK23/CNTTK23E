using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var multiMode = args.Any(a => a.Equals("--multi", StringComparison.OrdinalIgnoreCase));
var customRepoUrl = args.Length > 2 && !args[2].Equals("--multi", StringComparison.OrdinalIgnoreCase)
    ? args[2]
    : $"https://example.com/{studentCode}/signalr";

// Kenh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kenh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

if (multiMode)
{
    // Bai tap 3: Theo doi 2 luot nop cung luc tren cung mot ket noi
    var trackers = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

    connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
    {
        var shortId = m.SubmissionId.ToString()[..8];
        if (m.Status == "failed")
        {
            PrintRedWarning($"!!! CANH BAO: luot nop {shortId} cham LOI (failed)");
        }
        else
        {
            Console.WriteLine($"Thong bao SignalR: {shortId} -> {m.Status}");
        }

        if (m.Status is "graded" or "failed" && trackers.TryGetValue(m.SubmissionId, out var tcs))
        {
            tcs.TrySetResult(m.Status);
        }
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(a => a.IsOpen) ?? assignments?.FirstOrDefault();
    if (selected is null)
    {
        Console.WriteLine("Khong co bai tap nao.");
        return;
    }

    var sub1 = await CreateSubmissionAsync(http, selected.Id, "SV001", "https://example.com/sv001/signalr");
    var sub2 = await CreateSubmissionAsync(http, selected.Id, "SV002", "http://example.com/sv002/signalr");
    if (sub1 is null || sub2 is null) return;

    Console.WriteLine($"Da tao SV001: {sub1.Id.ToString()[..8]} ({sub1.Status})");
    Console.WriteLine($"Da tao SV002: {sub2.Id.ToString()[..8]} ({sub2.Status})");

    var tcs1 = trackers.GetOrAdd(sub1.Id, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));
    var tcs2 = trackers.GetOrAdd(sub2.Id, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));

    await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
    await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

    var allDone = Task.WhenAll(tcs1.Task, tcs2.Task);
    var finished = await Task.WhenAny(allDone, Task.Delay(TimeSpan.FromSeconds(10)));
    if (finished != allDone)
    {
        Console.WriteLine("Qua thoi gian cho thong bao ket thuc.");
    }

    var latest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
    var latest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

    var sr1 = tcs1.Task.IsCompletedSuccessfully ? tcs1.Task.Result : "timeout";
    var sr2 = tcs2.Task.IsCompletedSuccessfully ? tcs2.Task.Result : "timeout";
    Console.WriteLine($"SV001: SignalR = {sr1}, REST = {latest1?.Status}");
    Console.WriteLine($"SV002: SignalR = {sr2}, REST = {latest2?.Status}");
    return;
}

// Luong chinh EduSubmit realtime (1 luot nop)
var gradedNotification = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    if (m.Status == "failed")
    {
        PrintRedWarning($"!!! CANH BAO: luot nop {m.SubmissionId} cham LOI (failed)");
        gradedNotification.TrySetResult(m.Status);
    }
    else
    {
        Console.WriteLine($"Thong bao SignalR: {m.SubmissionId} -> {m.Status} luc {m.UpdatedAt.ToLocalTime():HH:mm:ss}");
        if (m.Status == "graded")
        {
            gradedNotification.TrySetResult(m.Status);
        }
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// 1. REST: lay danh sach bai tap
var assignmentList = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var chosen = assignmentList?.FirstOrDefault(a => a.IsOpen) ?? assignmentList?.FirstOrDefault();
if (chosen is null)
{
    Console.WriteLine("Khong tim thay bai tap nao de nop.");
    return;
}
Console.WriteLine($"Chon bai tap: {chosen.Id} - {chosen.Title}");

// 2. REST: tao luot nop
var created = await CreateSubmissionAsync(http, chosen.Id, studentCode, customRepoUrl);
if (created is null) return;

Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 3. SignalR: vao nhom theo doi dung luot nop
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// 4. Cho thong bao ket thuc (graded/failed) hoac het han 10 giay
var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// 5. Quy tac vang: doc lai trang thai bang REST
var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

static async Task<SubmissionDto?> CreateSubmissionAsync(
    HttpClient http,
    int assignmentId,
    string studentCode,
    string repositoryUrl)
{
    var response = await http.PostAsJsonAsync(
        $"/api/assignments/{assignmentId}/submissions",
        new CreateSubmissionRequest(studentCode, repositoryUrl));

    if (response.StatusCode != HttpStatusCode.Created)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Loi tao luot nop ({(int)response.StatusCode}): {error?.Code} - {error?.Message}");
        return null;
    }

    return await response.Content.ReadFromJsonAsync<SubmissionDto>();
}

static void PrintRedWarning(string message)
{
    var previous = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ForegroundColor = previous;
}

record AssignmentDto(int Id, string Title, DateTimeOffset DueAt, bool IsOpen);
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
