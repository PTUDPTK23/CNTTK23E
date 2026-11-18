using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var student1 = args.Length > 1 ? args[1] : "dtc245200988";
var student2 = args.Length > 2 ? args[2] : "SV002";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var trackers = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
{
    var shortId = m.SubmissionId.ToString()[..8];
    Console.WriteLine($"Thong bao SignalR: {shortId} -> {m.Status}");

    if (m.Status == "failed")
    {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {shortId} cham LOI (failed)");
        Console.ForegroundColor = prevColor;
    }

    if (m.Status is "graded" or "failed")
    {
        if (trackers.TryGetValue(m.SubmissionId, out var tcs))
            tcs.TrySetResult(m.Status);
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault(a => a.IsOpen);
if (selected is null) return;

var res1 = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(student1, $"https://example.com/{student1}/signalr"));
var sub1 = await res1.Content.ReadFromJsonAsync<SubmissionDto>();
var tcs1 = new TaskCompletionSource<string>();
trackers[sub1!.Id] = tcs1;
Console.WriteLine($"Da tao {student1}: {sub1.Id.ToString()[..8]} ({sub1.Status})");

var res2 = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(student2, $"http://example.com/{student2}/signalr"));
var sub2 = await res2.Content.ReadFromJsonAsync<SubmissionDto>();
var tcs2 = new TaskCompletionSource<string>();
trackers[sub2!.Id] = tcs2;
Console.WriteLine($"Da tao {student2}: {sub2.Id.ToString()[..8]} ({sub2.Status})");

await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

var allDone = Task.WhenAll(tcs1.Task, tcs2.Task);
await Task.WhenAny(allDone, Task.Delay(TimeSpan.FromSeconds(10)));

var latest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
var latest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

Console.WriteLine($"{student1}: SignalR = {tcs1.Task.Result}, REST = {latest1?.Status}");
Console.WriteLine($"{student2}: SignalR = {tcs2.Task.Result}, REST = {latest2?.Status}");

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);