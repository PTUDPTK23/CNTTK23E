using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
string Option(string key, string fallback)
{
    var index = Array.IndexOf(args, key);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
}
var hubPath = Option("--hub", "/hubs/submissions");
var eventName = Option("--event", "SubmissionStatusChanged");
if (!int.TryParse(Option("--watch-delay-ms", "0"), out var watchDelay) || watchDelay < 0)
{
    Console.Error.WriteLine("--watch-delay-ms phai la so nguyen khong am.");
    return 1;
}
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}{hubPath}").WithAutomaticReconnect().Build();
var gradedNotification = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
connection.On<SubmissionStatusMessage>(eventName, message =>
{
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt:HH:mm:ss}");
    if (message.Status == "graded") gradedNotification.TrySetResult();
});
try
{
    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");
    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault(a => a.IsOpen);
    if (selected is null) { Console.Error.WriteLine("Khong co bai tap dang mo."); return 1; }
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");
    var createResponse = await http.PostAsJsonAsync($"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode}/signalr"));
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        Console.Error.WriteLine($"HTTP {(int)createResponse.StatusCode}: {await createResponse.Content.ReadAsStringAsync()}");
        return 1;
    }
    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>()
        ?? throw new InvalidOperationException("POST khong tra ve luot nop.");
    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    if (watchDelay > 0) await Task.Delay(watchDelay);
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");
    if (await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10))) != gradedNotification.Task)
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
    return latest?.Status == "graded" ? 0 : 1;
}
catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
{
    Console.Error.WriteLine($"[Loi] {ex.Message}");
    return 1;
}

record AssignmentDto(int Id, string Title, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
