using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "dtc245200851";
var experiment = args.Length > 2 ? args[2] : "normal";
var hubPath = experiment == "wrong-hub" ? "/hub/submissions" : "/hubs/submissions";
var eventName = experiment == "wrong-event" ? "SubmissionStatusChange" : "SubmissionStatusChanged";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}{hubPath}").WithAutomaticReconnect().Build();
var gradedNotification = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Guid? watchedId = null;
connection.On<SubmissionStatusMessage>(eventName, message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");
    if (message.Status == "graded") gradedNotification.TrySetResult();
});
connection.Reconnected += async _ =>
{
    if (watchedId is null) return;
    await connection.InvokeAsync("WatchSubmission", watchedId.Value.ToString());
    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{watchedId}");
    Console.WriteLine($"REST sau ket noi lai: {latest?.Status}");
    if (latest?.Status == "graded") gradedNotification.TrySetResult();
};
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault(a => a.IsOpen);
if (selected is null) { Console.WriteLine("Khong co bai tap dang mo."); return; }
Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
using var response = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));
if (response.StatusCode != HttpStatusCode.Created)
{
    Console.Error.WriteLine(await response.Content.ReadAsStringAsync());
    Environment.ExitCode = 1;
    return;
}
var created = (await response.Content.ReadFromJsonAsync<SubmissionDto>())!;
Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
if (experiment == "late-watch") await Task.Delay(4000);
watchedId = created.Id;
await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
if (await Task.WhenAny(gradedNotification.Task, Task.Delay(10000)) != gradedNotification.Task)
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
var final = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {final?.Status}");
if (final?.Status != "graded") Environment.ExitCode = 1;

record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode,
    string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
