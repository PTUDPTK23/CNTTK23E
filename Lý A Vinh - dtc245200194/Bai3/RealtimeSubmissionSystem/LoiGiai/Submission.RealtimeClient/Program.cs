using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// Bai tap 3: Moi luot nop mot TaskCompletionSource trong ConcurrentDictionary
var trackers = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    var shortId = m.SubmissionId.ToString()[..8];
    if (m.Status == "failed")
    {
        // Bai tap 2: Canh bao mau do khi nhan trang thai failed
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {shortId} cham LOI (failed)");
        Console.ForegroundColor = prevColor;
    }
    else
    {
        Console.WriteLine($"Thong bao SignalR: {shortId} -> {m.Status}");
    }

    if (m.Status is "graded" or "failed")
    {
        if (trackers.TryGetValue(m.SubmissionId, out var tcs))
        {
            tcs.TrySetResult(m.Status);
        }
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

// Tao 2 luot nop: SV001 (https:// -> graded) va SV002 (http:// -> failed)
async Task<SubmissionDto?> CreateSubmissionAsync(string studentCode, string repoUrl)
{
    var resp = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentCode, repoUrl));

    if (resp.StatusCode != HttpStatusCode.Created)
    {
        var err = await resp.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Loi tao luot nop {studentCode}: {err?.Code} - {err?.Message}");
        return null;
    }

    var dto = await resp.Content.ReadFromJsonAsync<SubmissionDto>();
    if (dto is not null)
    {
        trackers[dto.Id] = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Console.WriteLine($"Da tao {studentCode}: {dto.Id.ToString()[..8]} ({dto.Status})");
    }
    return dto;
}

var sub1 = await CreateSubmissionAsync("SV001", "https://example.com/sv001/signalr");
var sub2 = await CreateSubmissionAsync("SV002", "http://example.com/sv002/signalr");
if (sub1 is null || sub2 is null) return;

// Watch ca hai luot nop tren cung mot ket noi SignalR
await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

var allDone = Task.WhenAll(trackers[sub1.Id].Task, trackers[sub2.Id].Task);
var completed = await Task.WhenAny(allDone, Task.Delay(TimeSpan.FromSeconds(10)));
if (completed != allDone)
{
    Console.WriteLine("Qua thoi gian cho thong bao cuoi cung.");
}

var signalr1 = trackers[sub1.Id].Task.IsCompletedSuccessfully ? trackers[sub1.Id].Task.Result : "timeout";
var signalr2 = trackers[sub2.Id].Task.IsCompletedSuccessfully ? trackers[sub2.Id].Task.Result : "timeout";

var rest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
var rest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

Console.WriteLine($"SV001: SignalR = {signalr1}, REST = {rest1?.Status}");
Console.WriteLine($"SV002: SignalR = {signalr2}, REST = {rest2?.Status}");

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

